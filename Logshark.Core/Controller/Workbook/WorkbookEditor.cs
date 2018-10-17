using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Logshark.ConnectionModel.Postgres;
using Optional;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Logshark.Core.Controller.Workbook
{
    /// <summary>
    /// Handles manipulation of workbook files.
    /// </summary>
    public sealed class WorkbookEditor
    {
        private readonly string outputDirectory;
        private readonly Option<PostgresConnectionInfo> postgresConnectionInfo;
        private readonly string databaseName;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public WorkbookEditor(string outputDirectory, Option<PostgresConnectionInfo> postgresConnectionInfo, string databaseName)
        {
            this.outputDirectory = outputDirectory;
            this.postgresConnectionInfo = postgresConnectionInfo;
            this.databaseName = databaseName;
        }

        public FileInfo Process(Stream workbook, string workbookName)
        {
            var workbookExtension = Path.GetExtension(workbookName);
            if (String.IsNullOrEmpty(workbookExtension))
            {
                throw new ArgumentException(String.Format("Invalid workbook name provided: workbook '{0} must have a valid file extension", workbookName), "workbookName");
            }

            string processedWorkbookFilePath;
            if (workbookExtension.Equals(".twbx", StringComparison.OrdinalIgnoreCase))
            {
                processedWorkbookFilePath = ProcessPackagedWorkbook(workbook, workbookName);
            }
            else if (workbookExtension.Equals(".twb", StringComparison.OrdinalIgnoreCase))
            {
                processedWorkbookFilePath = ProcessWorkbook(workbook, workbookName);
            }
            else
            {
                throw new ArgumentException(String.Format("Workbook '{0}' is not a supported Tableau workbook file type!", workbookName));
            }

            return new FileInfo(processedWorkbookFilePath);
        }

        private string ProcessPackagedWorkbook(Stream workbook, string workbookName)
        {
            using (var packagedWorkbook = new MemoryStream())
            {
                // Create an in-memory copy of the zip and configure it to be updatable (also in-memory)
                workbook.CopyTo(packagedWorkbook);
                var zipFile = new ZipFile(packagedWorkbook) { IsStreamOwner = false };
                zipFile.BeginUpdate();

                UpdatePackagedWorkbook(zipFile, workbookName);
                ReplacePackagedExtracts(zipFile, workbookName);
                PurgeCachedExtractContent(zipFile);

                zipFile.CommitUpdate();
                zipFile.Close();

                // Flush updated archive stream to disk
                string workbookFilePath = Path.Combine(outputDirectory, workbookName);
                using (var output = File.OpenWrite(workbookFilePath))
                {
                    packagedWorkbook.Position = 0;
                    packagedWorkbook.CopyTo(output);
                }

                return workbookFilePath;
            }
        }

        private string ProcessWorkbook(Stream workbook, string workbookName)
        {
            var workbookXml = UpdateWorkbookXml(workbook);

            string workbookFilePath = Path.Combine(outputDirectory, workbookName);

            workbookXml.Save(workbookFilePath);

            return workbookFilePath;
        }

        private void UpdatePackagedWorkbook(ZipFile zipFile, string workbookName)
        {
            // Locate the TWB.  There should only ever be one in a valid packaged workbook
            var workbookArchiveEntry = FindPackagedWorkbooks(zipFile).FirstOrDefault();
            if (workbookArchiveEntry == default(ZipEntry))
            {
                throw new ArgumentException(String.Format("Packaged workbook '{0}' contains no inner workbook!", workbookName));
            }

            using (var workbookStream = zipFile.GetInputStream(workbookArchiveEntry))
            {
                XmlDocument workbookXml = UpdateWorkbookXml(workbookStream);
                zipFile.Add(new XmlDataSource(workbookXml), workbookArchiveEntry.Name);
            }
        }

        private XmlDocument UpdateWorkbookXml(Stream workbookStream)
        {
            XmlDocument workbookXml = new XmlDocument();
            workbookXml.Load(workbookStream);

            var editor = new WorkbookXmlEditor(workbookXml);

            postgresConnectionInfo.MatchSome(postgresConnection => editor.UpdatePostgresConnections(postgresConnection, databaseName));            
            editor.RemoveThumbnails();

            return editor.WorkbookXml;
        }

        private ZipFile ReplacePackagedExtracts(ZipFile zipFile, string workbookName)
        {
            var packagedExtracts = FindPackagedExtracts(zipFile);
            var generatedExtracts = FindAvailableExtracts(outputDirectory).ToDictionary(Path.GetFileName, path => path);

            foreach (ZipEntry packagedExtract in packagedExtracts)
            {
                string extractName = Path.GetFileName(packagedExtract.Name);
                if (!String.IsNullOrEmpty(extractName) && generatedExtracts.ContainsKey(extractName))
                {
                    zipFile.Add(generatedExtracts[extractName], packagedExtract.Name);
                    Log.DebugFormat("Replaced extract '{0}' in workbook '{1}'", extractName, workbookName);
                }
            }

            return zipFile;
        }

        private static IEnumerable<ZipEntry> FindPackagedWorkbooks(ZipFile packagedWorkbook)
        {
            return packagedWorkbook.Cast<ZipEntry>().Where(archiveEntry => archiveEntry.IsFile && archiveEntry.Name.EndsWith(".twb"));
        }

        private static IEnumerable<ZipEntry> FindPackagedExtracts(ZipFile packagedWorkbook)
        {
            return packagedWorkbook.Cast<ZipEntry>().Where(archiveEntry => archiveEntry.IsFile && archiveEntry.Name.EndsWith(".hyper", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> FindAvailableExtracts(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.hyper", SearchOption.AllDirectories);
        }

        private static ZipFile PurgeCachedExtractContent(ZipFile zipFile)
        {
            foreach (ZipEntry cachedExtractContentEntry in zipFile.Cast<ZipEntry>().Where(entry => entry.Name.StartsWith("TwbxExternalCache")))
            {
                zipFile.Delete(cachedExtractContentEntry);
            }

            return zipFile;
        }

        /// <summary>
        /// Utility class used by SharpZipLib
        /// </summary>
        private sealed class XmlDataSource : IStaticDataSource
        {
            private readonly XmlDocument xml;

            public XmlDataSource(XmlDocument xml)
            {
                this.xml = xml;
            }
            
            public Stream GetSource()
            {
                var stream = new MemoryStream();

                xml.Save(stream);
                stream.Position = 0;

                return stream;
            }
        }
    }
}