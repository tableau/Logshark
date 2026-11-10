using LogShark.Extensions;
using LogShark.HostExtensions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogShark.Metrics;
using LogShark.Plugins;
using LogShark.Shared.Common;
using LogShark.Shared.HostExtensions;
using EnvironmentController = LogShark.Common.EnvironmentController;
using Flurl.Util;

namespace LogShark
{
    [Command(
    ExtendedHelpText = @"
Usage Examples:
  logshark D:\logs.zip                                                  | Runs logshark on logs.zip and outputs locally.
  logshark C:\logs\logset --plugins ""Backgrounder;ClusterController""    | Runs specified plugins on existing unzipped log directory.
  logshark logs.zip -p                                                  | Runs logshark and publishes to Tableau Server.
  logshark logs.zip -c CustomLogSharkConfig.json                        | Runs logshark with a custom config file.
  logshark logs.zip --plugins ""Apache;MyPlugin"" --dynamic-plugins-dir C:\plugins | Loads external plugins from directory.

Dynamic Plugins:
  External plugins can be loaded at runtime from a directory of .NET assemblies.
  Each plugin must reference LogShark.Shared and implement the IPlugin interface.
  Use --dynamic-plugins-dir to specify the directory, or set DynamicPluginsDirectory
  in LogSharkConfig.json. Relative paths are resolved from the LogShark installation
  directory, not the current working directory - use absolute paths to avoid confusion.
  If a dynamic plugin has the same name as a built-in plugin, the built-in takes precedence.
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

        [Option("--pg-db-port", Description = "Output database port for postgres writer")]
        public string SqlDbPort { get; set; }

        [Option("--pg-db-user", Description = "Output database username for postgres writer")]
        public string SqlDbUser { get; set; }
        
        [Option("--pg-db-pass", Description = "Output database password for postgres writer")]
        public string SqlDbPassword { get; set; }

        [Option("--pg-embed-creds", Description = "Embed credentials in workbook on publish")]
        public bool SqlEmbedCreds { get; set; }
        
        [Option("-t|--threads", Description = "Number of processing threads to run in parallel")]
        public int? NumberOfThreads { get; set; }

        [Option("--progress", Description = "Display real-time progress with ETA estimates")]
        public bool ShowProgress { get; set; }

        [Option("--dynamic-plugins-dir", Description = "Directory containing dynamically loaded plugin assemblies. Absolute paths recommended; relative paths resolve from the LogShark installation directory, not the working directory")]
        public string DynamicPluginsDirectory { get; set; }

        [Option("--adhoc", Description = "Adhoc mode: path to a directory of loose Tableau log files (read recursively). Files are classified by filename and the worker id is taken from the filename. Usage: logshark --adhoc <dir> [<runId>]")]
        public string AdhocDir { get; set; }

        #endregion Command Line Arguments

        private async Task<int> OnExecute()
        {
            var exeDir = AppContext.BaseDirectory;
            ILogSharkHostExtension hostExtension;
            IConfigurationRoot configuration;
            try
            {
                hostExtension = HostExtensionLoader.Load(exeDir, NullLogger.Instance);
                configuration = LogSharkConfigurationLoader.Build(exeDir, Config ?? "Config/LogSharkConfig.json", hostExtension?.ConfigOverlayPath);
            }
            catch (HostExtensionLoadException ex)
            {
                // Logging is not configured yet (it depends on the overlay this extension provides)
                await Console.Error.WriteLineAsync(ex.Message);
                return EnvironmentController.SetExitCode(ExitCode.ERROR);
            }

            var loggerFactory = ConfigureLogging(configuration, ShowProgress, LogSetLocation);
            var logger = loggerFactory.CreateLogger<Program>();
            if (hostExtension == null)
            {
                logger.LogDebug("No host extension loaded; artifact publishing disabled");
            }
            else
            {
                logger.LogInformation("Host extension loaded: {hostExtensionType}", hostExtension.GetType().FullName);
            }

            // Adhoc mode: --adhoc holds the directory of loose files. In that case the first positional
            // argument (normally the logset location) is treated as the optional run id instead.
            var adhocMode = !string.IsNullOrWhiteSpace(AdhocDir);
            var effectiveLogSetLocation = adhocMode ? AdhocDir : LogSetLocation;
            var effectiveRunId = RunId;
            if (adhocMode && string.IsNullOrWhiteSpace(effectiveRunId) && !string.IsNullOrWhiteSpace(LogSetLocation))
            {
                effectiveRunId = LogSetLocation;
            }

            var clParameters = new LogSharkCommandLineParameters
            {
                AppendTo = AppendTo,
                DatabaseName = SqlDbName,
                DatabaseConnectionString = SqlDbConnectionString,
                DatabaseHost = SqlDbHost,
                DatabasePassword = SqlDbPassword,
                DatabasePort = SqlDbPort,
                DatabaseUsername = SqlDbUser,
                EmbedCredentialsOnPublish = SqlEmbedCreds,
                ForceRunId = ForceRunId,
                LogSetLocation = effectiveLogSetLocation,
                AdhocMode = adhocMode,
                NumberOfParallelThreads = NumberOfThreads,
                PublishWorkbooks = PublishWorkbooks,
                RequestedPlugins = RequestedPlugins,
                UserProvidedRunId = effectiveRunId,
                RequestedWriter = Writer,
                ShowProgress = ShowProgress,
                TableauServerUsername = TableauServerUsername,
                TableauServerPassword = TableauServerPassword,
                TableauServerSite = TableauServerSite,
                TableauServerUrl = TableauServerUrl,
                TableauServerProjectDescriptionFooterHtml = TableauServerProjectDescriptionFooterHtml,
                WorkbookNameSuffixOverride = WorkbookNameSuffixOverride,
                DynamicPluginsDirectory = DynamicPluginsDirectory,
            };
            var config = new LogSharkConfiguration(clParameters, configuration, loggerFactory);

            var metricUploaderConfiguration = new MetricsUploaderConfiguration(config);
            var metricUploader = new MetricUploader(metricUploaderConfiguration, loggerFactory);
            var metricsConfig = new MetricsConfig(metricUploader, config);
            var metricsModule = new MetricsModule(metricsConfig, loggerFactory);
            IArtifactPublisher artifactPublisher;
            try
            {
                artifactPublisher = HostExtensionLoader.CreateArtifactPublisher(hostExtension, configuration, loggerFactory);
            }
            catch (HostExtensionLoadException ex)
            {
                await Console.Error.WriteLineAsync(ex.Message);
                return EnvironmentController.SetExitCode(ExitCode.ERROR);
            }
            string filePath = config.HyperLogDir;
            string missedFileName = Path.Combine(filePath, $"MissedLines_{Path.GetFileNameWithoutExtension(effectiveLogSetLocation)}_{DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}.txt");
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
                else if (string.IsNullOrWhiteSpace(effectiveLogSetLocation))
                {
                    Console.WriteLine("The LogSetLocation field is required (or use --adhoc <dir>).\nSpecify --help for a list of available options and commands.");
                    return EnvironmentController.SetExitCode(ExitCode.ERROR);
                }
                else
                {
                    var runner = new LogSharkRunner(config, metricsModule, artifactPublisher, loggerFactory);
                    var runSummary = await runner.Run();
                    EnvironmentController.SetExitCode(runSummary, false);

                    logger.LogInformation(runSummary.ToStringReport());
                    runSummary.ProcessingErrorsLines(missedFileName);
                }
            }
            finally
            {
                loggerFactory.Dispose();
                Thread.Sleep(200); // Otherwise logger does not write final message sometimes
            }

            return Environment.ExitCode;
        }
        
        public static Progress.SpectreConsoleLoggerProvider SpectreLoggerProvider { get; private set; }

        private static ILoggerFactory ConfigureLogging(IConfiguration configRoot, bool showProgress, string logSetLocation)
        {
            var loggingConfig = configRoot.GetSection("Logging");

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                // When progress is enabled, use Spectre.Console-aware logger without any filtering
                if (showProgress)
                {
                    // Set minimum log level to Trace (lowest) to ensure no filtering
                    builder.SetMinimumLevel(LogLevel.Trace);
                    SpectreLoggerProvider = new Progress.SpectreConsoleLoggerProvider(LogLevel.Trace);
                    builder.AddProvider(SpectreLoggerProvider);
                }
                else
                {
                    // When not showing progress, use standard configuration with filters from config file
                    builder.AddConfiguration(loggingConfig);
                    builder.AddConsole();
                }
            });
            loggerFactory.AddProvider(new Logging.RollingFileLoggerProvider(loggingConfig, logSetLocation));
            return loggerFactory;
        }
    }
}