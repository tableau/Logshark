using LogShark.Containers;
using LogShark.Exceptions;
using LogShark.Writers.Containers;
using LogShark.Writers.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace LogShark.Writers.Sql
{
    public class SqlWorkbookGenerator : IWorkbookGenerator
    {
        private readonly ILogger _logger;
        private readonly LogSharkConfiguration _config;
        private readonly string _workbooksOutputDir;
        private readonly string _dbName;
        private readonly string _dbHost;
        private readonly string _dbPort;
        private readonly string _dbUsername;

        public bool GeneratesWorkbooks => true;

        public SqlWorkbookGenerator(
            string runId,
            LogSharkConfiguration config,
            string dbName,
            string dbHost,
            string dbPort,
            string dbUsername,
            ILoggerFactory loggerFactory)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<SqlWorkbookGenerator>();
            (_, _workbooksOutputDir) = OutputDirInitializer.InitDirs(_config.OutputDir, runId, _config.AppendTo, "postgres", loggerFactory, _config.ThrowIfOutputDirectoryExists);

            _dbName = dbName;
            _dbHost = dbHost;
            _dbPort = dbPort;
            _dbUsername = dbUsername;
        }

        public WorkbookGeneratorResults CompleteWorkbooksWithResults(WritersStatistics writersStatistics)
        {
            _logger.LogInformation("Starting to generate workbooks with results...");

            var availableTemplates = WorkbookGeneratorCommon.GenerateWorkbookTemplatesList(_config.WorkbookTemplatesDirectory, _config.CustomWorkbookTemplatesDirectory, _logger);
            var applicableTemplates = WorkbookGeneratorCommon.SelectTemplatesApplicableToThisRun(availableTemplates, writersStatistics);
            var nonEmptyExtractNames = WorkbookGeneratorCommon.GetNonEmptyExtractNames(writersStatistics);
            var results = new WorkbookGeneratorResults(new List<CompletedWorkbookInfo>(), availableTemplates);

            foreach (var template in applicableTemplates)
            {
                var templateWorkbookPath = template.Path;
                var hasAnyData = WorkbookGeneratorCommon.HasAnyData(template, nonEmptyExtractNames);

                var baseWorkbookName = template.Name + _config.CustomWorkbookSuffix;
                var finalWorkbookName = hasAnyData ? baseWorkbookName : $"{baseWorkbookName} [No Data]";
                var destinationWorkbookPath = Path.Combine(_workbooksOutputDir, $"{finalWorkbookName}.twb");

                try
                {
                    CopyWorkbookToOutput(templateWorkbookPath, destinationWorkbookPath);

                    ReplaceConnectionInWorkbook(destinationWorkbookPath);

                    results.CompletedWorkbooks.Add(new CompletedWorkbookInfo(template.Name, finalWorkbookName, destinationWorkbookPath, hasAnyData));
                }
                catch (Exception ex)
                {
                    var message = $"Exception occurred while generating workbook {template.Name}. Exception: {ex.Message}";
                    _logger.LogError(message);
                    results.CompletedWorkbooks.Add(new CompletedWorkbookInfo(template.Name, finalWorkbookName, workbookPath: null, hasAnyData: false, new WorkbookGeneratingException("", ex)));
                }
            }

            return results;
        }

        private static void CopyWorkbookToOutput(string templateWorkbookPath, string destinationWorkbookPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationWorkbookPath));

            if (templateWorkbookPath.EndsWith(".twbx"))
            {
                using (var zip = ZipFile.Open(templateWorkbookPath, ZipArchiveMode.Read))
                {
                    var workbookEntry = zip.Entries.First(e => e.Name.EndsWith(".twb"));
                    workbookEntry.ExtractToFile(destinationWorkbookPath, overwrite: true);
                }
            }
            else
            {
                File.Copy(templateWorkbookPath, destinationWorkbookPath);
            }
        }

        private void ReplaceConnectionInWorkbook(string finalWorkbookPath)
        {
            /* Replace this XML in the workbook

                <datasource caption='ApacheRequests Extract' inline='true' name='federated.0ol3tzd1nl7ehf17w7cax0n3xrz5' version='10.5'>
                  <connection class='federated'>
                    <named-connections>
                      <named-connection caption='ApacheRequests' name='hyper.0m7d4ma1qnl7g01gn43z20jsuuis'>
                        <connection authentication='auth-none' author-locale='en_US' class='hyper' dbname='Data/ApacheRequests.hyper' default-settings='yes' sslmode='' username='tableau_internal_user' />
                      </named-connection>
                    </named-connections>
                    <relation connection='hyper.0m7d4ma1qnl7g01gn43z20jsuuis' name='ApacheRequests' table='[public].[ApacheRequests]' type='table' />

               With this

                <datasource caption="ApacheRequests Table" inline="true" name="federated.0ol3tzd1nl7ehf17w7cax0n3xrz5" version="10.5">
                  <connection class="federated">
                    <named-connections>
                      <named-connection caption="ApacheRequests" name="postgres.0m7d4ma1qnl7g01gn43z20jsuuis">
                        <connection authentication="username-password" class="postgres" dbname="logshark-test" odbc-native-protocol="" one-time-sql="" port="5433" server="localhost" username="postgres" />
                      </named-connection>
                    </named-connections>
                    <relation connection="postgres.0m7d4ma1qnl7g01gn43z20jsuuis" name="ApacheRequests" table="[public].[ApacheRequests]" type="table" />
             */

            var workbookXml = XDocument.Load(finalWorkbookPath);

            var dataSources = workbookXml.Descendants("datasources")
                                         .Descendants("datasource")
                                         .Where(n => (n.Attribute("inline")?.Value ?? "") == "true"
                                                  && (n.Attribute("name")?.Value ?? "").StartsWith("federated"));

            foreach (var dataSource in dataSources)
            {
                var connections = dataSource.Descendants("connection")
                                            .Where(n => (n.Attribute("class")?.Value ?? "") == "federated");

                // Call it "DataSource Table" instead of "DataSource Extract"
                // Some data sources don't have " Extract" in their captions and I don't know why
                var caption = dataSource.Attribute("caption").Value.Replace(" Extract", "") + " Table";
                dataSource.SetAttributeValue("caption", caption);

                var hyperConnections = connections.Descendants("named-connections")
                                                  .Descendants("named-connection")
                                                  .Where(e => (e.Attribute("name").Value ?? "").StartsWith("hyper"));

                var replacements = new Dictionary<XElement, XElement>();
                foreach (var hyperConnection in hyperConnections)
                {
                    var hyperConnectionName = hyperConnection.Attribute("name").Value;
                    var postgresConnectionName = hyperConnectionName.Replace("hyper", "postgres");

                    var originalCaption = hyperConnection.Attribute("caption").Value;

                    var postgresConnection = new XElement("named-connection",
                                                new XAttribute("caption", originalCaption),
                                                new XAttribute("name", postgresConnectionName),
                                               new XElement("connection",
                                                  new XAttribute("authentication", "username-password"),
                                                  new XAttribute("class", "postgres"),
                                                  new XAttribute("dbname", _dbName),
                                                  new XAttribute("odbc-native-protocol", ""),
                                                  new XAttribute("one-time-sql", ""),
                                                  new XAttribute("port", _dbPort),
                                                  new XAttribute("server", _dbHost),
                                                  new XAttribute("username", _dbUsername)));

                    replacements.Add(hyperConnection, postgresConnection);

                    var hyperRelations = connections.Descendants("relation")
                                                    .Where(e => e.Attribute("connection")?.Value == hyperConnectionName);

                    foreach (var hyperRelation in hyperRelations)
                    {
                        var postgresRelation = new XElement(hyperRelation);
                        postgresRelation.SetAttributeValue("connection", postgresConnectionName);

                        replacements.Add(hyperRelation, postgresRelation);
                    }
                }

                // Can't do this in the above loop as it modifies the results of the Enumerable
                foreach (var replacement in replacements)
                {
                    replacement.Key.ReplaceWith(replacement.Value);
                }
            }

            workbookXml.Save(finalWorkbookPath);
        }
    }
}
