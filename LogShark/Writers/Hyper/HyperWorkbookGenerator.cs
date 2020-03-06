using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;

namespace LogShark.Writers.Hyper
{
    public class HyperWorkbookGenerator : IWorkbookGenerator
    {
        private readonly LogSharkConfiguration _config;
        private readonly ILogger _logger;
        private readonly string _hyperOutputDir;
        private readonly string _workbooksOutputDir;

        public bool GeneratesWorkbooks => true;

        public HyperWorkbookGenerator(LogSharkConfiguration config, ILogger logger, string hyperOutputDir, string workbooksOutputDir)
        {
            _config = config;
            _logger = logger;
            _hyperOutputDir = hyperOutputDir;
            _workbooksOutputDir = workbooksOutputDir;
        }

        public WorkbookGeneratorResults CompleteWorkbooksWithResults(WritersStatistics writersStatistics)
        {
            _logger.LogInformation("Starting to generate workbooks with results...");

            var availableTemplates = WorkbookGeneratorCommon.GenerateWorkbookTemplatesList(_config.WorkbookTemplatesDirectory, _config.CustomWorkbookTemplatesDirectory, _logger);
            var applicableTemplates = WorkbookGeneratorCommon.SelectTemplatesApplicableToThisRun(availableTemplates, writersStatistics);
            var nonEmptyExtractNames = WorkbookGeneratorCommon.GetNonEmptyExtractNames(writersStatistics);
            
            var completedWorkbooks = applicableTemplates
                .Select(template => CompleteWorkbook(template, WorkbookGeneratorCommon.HasAnyData(template, nonEmptyExtractNames)))
                .ToList();
            
            return new WorkbookGeneratorResults(completedWorkbooks, availableTemplates);
        }

        private CompletedWorkbookInfo CompleteWorkbook(PackagedWorkbookTemplateInfo templateInfo, bool hasAnyData)
        {
            var workbookNameToUse = hasAnyData
                ? $"{templateInfo.Name}{_config.CustomWorkbookSuffix}"
                : $"{templateInfo.Name}{_config.CustomWorkbookSuffix} [No Data]";

            try
            {
                // Copy the template to where we want the workbook to live
                var finalWorkbookPath = Path.Combine(_workbooksOutputDir, $"{workbookNameToUse}.twbx");

                // In the case of custom workbooks, we don't have a custom folder in our output. Create one if we need to
                Directory.CreateDirectory(Path.GetDirectoryName(finalWorkbookPath));

                File.Copy(templateInfo.Path, finalWorkbookPath);

                // Replace necessary hyper files in the workbook
                ProcessPackagedWorkbook(finalWorkbookPath);
                _logger.LogDebug("Workbook {completedWorkbookName} complete", templateInfo.Name);
                return new CompletedWorkbookInfo(templateInfo, workbookNameToUse, finalWorkbookPath, hasAnyData);
            }
            catch (Exception ex)
            {
                var message = $"Exception occurred while generating workbook {templateInfo.Name}. Exception: {ex.Message}";
                _logger.LogError(message);
                return CompletedWorkbookInfo.GetFailedInfo(templateInfo, new WorkbookGeneratingException(message, ex));
            }
        }

        private void ProcessPackagedWorkbook(string workbookPath)
        {
            var generatedExtracts = FindAvailableExtracts().ToDictionary(Path.GetFileName, path => path);
            var tempZipArchivePath = workbookPath + ".tmp";

            // This awkward dance is so that we can create twbxes with extracts larger than 2GB
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.zipfileextensions.createentryfromfile?view=netcore-2.2
            //  "When ZipArchiveMode.Update is present, the size limit of an entry is limited to Int32.MaxValue"
            using (var sourceWorkbook = ZipFile.Open(workbookPath, ZipArchiveMode.Read))
            using (var tempZipArchive = ZipFile.Open(tempZipArchivePath, ZipArchiveMode.Create))
            {
                foreach (var entry in sourceWorkbook.Entries)
                {
                    var destEntry = tempZipArchive.CreateEntry(entry.FullName);

                    Stream srcStream;
                    if (generatedExtracts.ContainsKey(entry.Name))
                    {
                        var extractFileName = generatedExtracts[entry.Name];

                        // Hyper files stay locked for a few moments sometimes. If we keep hitting it - need to investigate further
                        WaitUntilExtractFileIsReady(extractFileName);

                        srcStream = File.OpenRead(extractFileName);
                    }
                    else
                    {
                        srcStream = entry.Open();
                    }

                    using (var destStream = destEntry.Open())
                    {
                        srcStream.CopyTo(destStream);
                    }

                    srcStream.Dispose();
                }
            }

            File.Delete(workbookPath);
            File.Move(tempZipArchivePath, workbookPath);
        }

        private IEnumerable<string> FindAvailableExtracts()
        {
            return Directory.GetFiles(_hyperOutputDir, "*.hyper", SearchOption.AllDirectories);
        }

        private static void WaitUntilExtractFileIsReady(string filename)
        {
            for (var i = 0; i < 10; ++i)
            {
                if (IsFileReady(filename))
                {
                    return;
                }
                            
                Thread.Sleep(250);
            }
            
            throw new WorkbookGeneratingException($"Cannot open file {filename} for read within allowed amount of time");
        }

        private static bool IsFileReady(string filename)
        {
            try
            {
                using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
