using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace LogShark.Writers
{
    public static class WorkbookGeneratorCommon
    {
        public static IList<PackagedWorkbookTemplateInfo> GenerateWorkbookTemplatesList(string workbookTemplatesDirectory, string customWorkbookTemplatesDirectory, ILogger logger)
        {
            var response = new List<PackagedWorkbookTemplateInfo>();
            if (Directory.Exists(workbookTemplatesDirectory))
            {
                response.AddRange(Directory.GetFiles(workbookTemplatesDirectory)
                    .Where(name => name.EndsWith(".twbx"))
                    .Select(file => GetPackagedWorkbookTemplateInfo(file, string.Empty))
                    .ToList());
            }
            else
            {
                logger.LogError("Directory {missingWorkbookTemplateDir} does not exist. Use configuration file to specify correct directory with workbook templates", workbookTemplatesDirectory);
            }

            // If we have a custom workbook location provided, add all the twbx we find there
            // If the directory doesn't exist, log a warning
            if (!string.IsNullOrWhiteSpace(customWorkbookTemplatesDirectory))
            {
                if (Directory.Exists(customWorkbookTemplatesDirectory))
                {
                    response.AddRange(Directory.GetFiles(customWorkbookTemplatesDirectory)
                        .Where(name => name.EndsWith(".twbx"))
                        .Select(file => GetPackagedWorkbookTemplateInfo(file, "Custom"))
                        .ToList());
                }
                else
                {
                    logger.LogWarning("Directory {_customWorkbookTemplatesDirectory} was configured but does not exist. Use configuration file to specify directory with workbook templates or remove value from config.", customWorkbookTemplatesDirectory);
                }
            }

            return response;
        }

        /// <summary>
        /// Filters the list of all available templates leaving only templates which can be used with the data generated writers in this run
        /// </summary>
        public static IEnumerable<PackagedWorkbookTemplateInfo> SelectTemplatesApplicableToThisRun(IEnumerable<PackagedWorkbookTemplateInfo> allTemplates, WritersStatistics writersStatistics)
        {
            var availableDataSets = writersStatistics.DataSets
                .Select(kvp => kvp.Key.Name)
                .Select(name => $"{name}.hyper") // PackagedWorkbookTemplateInfo has hyper file names in it
                .ToHashSet();

            return allTemplates.Where(template => HaveAllDataSets(template, availableDataSets));
        }

        public static HashSet<string> GetNonEmptyExtractNames(WritersStatistics writersStatistics)
        {
            return writersStatistics.DataSets
                .Where(pair => pair.Value.LinesPersisted > 0)
                .Select(pair => pair.Key.Name + ".hyper")
                .ToHashSet();
        }

        /// <summary>
        /// This checks if at least one of the data sources used by the workbook has at least one row of data
        /// </summary>
        public static bool HasAnyData(PackagedWorkbookTemplateInfo templateInfo, ICollection<string> nonEmptyExtractNames)
        {
            return templateInfo.RequiredExtracts.Any(nonEmptyExtractNames.Contains);
        }
        
        private static PackagedWorkbookTemplateInfo GetPackagedWorkbookTemplateInfo(string twbxPath, string folderPrefix)
        {
            using (var zipArchive = ZipFile.Open(twbxPath, ZipArchiveMode.Read))
            {
                var name = Path.Join(folderPrefix, Path.GetFileNameWithoutExtension(twbxPath));
                var extractFiles = zipArchive.Entries
                    .Where(entry => entry.Name.EndsWith(".hyper"))
                    .Select(entry => entry.Name)
                    .ToHashSet();
                return new PackagedWorkbookTemplateInfo(name, twbxPath, extractFiles);
            }
        }
        
        /// <summary>
        /// This checks if we have ALL files required by workbook (whatever they are empty or not doesn't matter).
        /// We must have all files in order for workbook to at least open, so if at least one file is missing - there is no point in generating workbook at all.
        /// </summary>
        private static bool HaveAllDataSets(PackagedWorkbookTemplateInfo templateInfo, ICollection<string> availableExtractFiles)
        {
            return templateInfo.RequiredExtracts.All(availableExtractFiles.Contains);
        }
    }
}
