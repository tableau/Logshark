﻿using LogShark.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogShark.Metrics;
using LogShark.Plugins;
using LogShark.Shared.Common;
using EnvironmentController = LogShark.Common.EnvironmentController;

namespace LogShark
{
    [Command(
    ExtendedHelpText = @"
Usage Examples:
  logshark D:\logs.zip                                                  | Runs logshark on logs.zip and outputs locally.
  logshark C:\logs\logset --plugins ""Backgrounder;ClusterController""    | Runs specified plugins on existing unzipped log directory.
  logshark logs.zip -p                                                  | Runs logshark and publishes to Tableau Server.
  logshark logs.zip -c CustomLogSharkConfig.json                        | Runs logshark with a custom config file.
"
)]
    class Program
    {
        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        #region Command Line Arguments

        [Argument(0, Description = "Location of the logs to process (zip file or unzipped folder)")]
        public string LogSetLocation { get; }

        [Argument(1, Description = "Unique identifier to use for output of this run (i.e. SaturdayOutageLogs)")]
        public string RunId { get; }

        [Option(Description = "Append this run results to the results from specified run id. Implementation varies by output writer. See documentation for more info")]
        public string AppendTo { get; set; }

        [Option(Description = "Specify alternative config file to use. By default config/LogSharkConfig.json is used")]
        public string Config { get; set; }
        
        [Option("--force-run-id", Description = "LogShark prefixes RunId with timestamp even if RunId provided by user. This flag prevents timestamps from being added to user-supplied RunId. In this case you are responsible for providing unique RunId for each run ")]
        public bool ForceRunId { get; set; }

        [Option("-p|--publishworkbooks", Description = "Publish to Tableau Server")]
        public bool PublishWorkbooks { get; set; }

        [Option("--plugins", Description = "List of plugins to run, semicolon separated, no spaces. Or \"All\" to run all applicable plugins. Not specifying plugins works as \"All\"")]
        public string RequestedPlugins { get; set; }

        [Option("-l|--listplugins", Description = "Lists the LogShark plugins available for use with '--plugins' parameter")]
        public bool ListPLugins { get; set; }

        [Option(Description = "Select type of output writer to use (i.e. \"csv\")")]
        public string Writer { get; set; }

        [Option("--username", Description = "Tableau Server username.")]
        public string TableauServerUsername { get; set; }

        [Option("--password", Description = "Tableau Server password.")]
        public string TableauServerPassword { get; set; }
        
        [Option("--ts-project-description-footer", Description = "Additional text (HTML allowed) to append to the description of the project created on Tableau Server")]
        public string TableauServerProjectDescriptionFooterHtml { get; set; }

        [Option("--site", Description = "Tableau Server site name.")]
        public string TableauServerSite { get; set; }

        [Option("--url", Description = "Tableau Server url.")]
        public string TableauServerUrl { get; set; }

        [Option("--workbookname", Description = "Custom workbook name to append to the end of each workbook generated.")]
        public string WorkbookNameSuffixOverride { get; set; }
        
        [Option("--pg-db-conn-string", Description = "Connection string for output database for postgres writer")]
        public string SqlDbConnectionString { get; set; }
        
        [Option("--pg-db-host", Description = "Output database hostname for postgres writer")]
        public string SqlDbHost { get; set; }
        
        [Option("--pg-db-name", Description = "Output database name for postgres writer")]
        public string SqlDbName { get; set; }
        
        [Option("--pg-db-user", Description = "Output database username for postgres writer")]
        public string SqlDbUser { get; set; }
        
        [Option("--pg-db-pass", Description = "Output database password for postgres writer")]
        public string SqlDbPassword { get; set; }

        [Option("--pg-embed-creds", Description = "Embed credentials in workbook on publish")]
        public bool SqlEmbedCreds { get; set; }
        
        [Option("-t|--threads", Description = "Number of processing threads to run in parallel")]
        public int? NumberOfThreads { get; set; }

        #endregion Command Line Arguments

        private async Task<int> OnExecute()
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(exeDir)
                .AddJsonFile(Config ?? "Config/LogSharkConfig.json", optional: false, reloadOnChange: false)
                .Build();

            var loggerFactory = ConfigureLogging(configuration);
            var logger = loggerFactory.CreateLogger<Program>();

            var clParameters = new LogSharkCommandLineParameters
            {
                AppendTo = AppendTo,
                DatabaseName = SqlDbName,
                DatabaseConnectionString = SqlDbConnectionString,
                DatabaseHost = SqlDbHost,
                DatabasePassword = SqlDbPassword,
                DatabaseUsername = SqlDbUser,
                EmbedCredentialsOnPublish = SqlEmbedCreds,
                ForceRunId = ForceRunId,
                LogSetLocation = LogSetLocation,
                NumberOfParallelThreads = NumberOfThreads,
                PublishWorkbooks = PublishWorkbooks,
                RequestedPlugins = RequestedPlugins,
                UserProvidedRunId = RunId,
                RequestedWriter = Writer,
                TableauServerUsername = TableauServerUsername,
                TableauServerPassword = TableauServerPassword,
                TableauServerSite = TableauServerSite,
                TableauServerUrl = TableauServerUrl,
                TableauServerProjectDescriptionFooterHtml = TableauServerProjectDescriptionFooterHtml,
                WorkbookNameSuffixOverride = WorkbookNameSuffixOverride,
            };
            var config = new LogSharkConfiguration(clParameters, configuration, loggerFactory);

            var metricUploaderConfiguration = new MetricsUploaderConfiguration(config);
            var metricUploader = new MetricUploader(metricUploaderConfiguration, loggerFactory);
            var metricsConfig = new MetricsConfig(metricUploader, config);
            var metricsModule = new MetricsModule(metricsConfig, loggerFactory);

            try
            {
                if (ListPLugins)
                {
                    using (var pluginManager = new PluginManager(config, loggerFactory))
                    {
                        var plugins = string.Join("\n\t- ", pluginManager.GetKnownPluginNames());
                        Console.WriteLine($"Available plugins:\n\t- {plugins}");
                    }
                    return EnvironmentController.SetExitCode(ExitCode.OK);
                }
                else if (string.IsNullOrWhiteSpace(LogSetLocation))
                {
                    Console.WriteLine("The LogSetLocation field is required.\nSpecify --help for a list of available options and commands.");
                    return EnvironmentController.SetExitCode(ExitCode.ERROR);
                }
                else
                {
                    var runner = new LogSharkRunner(config, metricsModule, loggerFactory);
                    var runSummary = await runner.Run();
                    EnvironmentController.SetExitCode(runSummary, false);

                    logger.LogInformation(runSummary.ToStringReport());
                }
            }
            finally
            {
                loggerFactory.Dispose();
                Thread.Sleep(200); // Otherwise logger does not write final message sometimes
            }

            return Environment.ExitCode;
        }
        
        private static ILoggerFactory ConfigureLogging(IConfiguration configRoot)
        {
            var loggingConfig = configRoot.GetSection("Logging");
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConfiguration(loggingConfig)
                    .AddConsole();
            });
            loggerFactory.AddFile(loggingConfig);
            return loggerFactory;
        }
    }
}