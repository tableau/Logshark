using LogShark.Extensions;
using LogShark.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LogShark.Shared.Extensions;

namespace LogShark
{
    public class LogSharkConfiguration
    {
        private readonly LogSharkCommandLineParameters _parameters;
        private readonly IConfiguration _config;
        private readonly string _rootDir;
        private readonly ILogger _logger;

        public LogSharkConfiguration(LogSharkCommandLineParameters parameters, IConfiguration config, ILoggerFactory loggerFactory)
        {
            _parameters = parameters;
            _config = config;
            _rootDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _logger = loggerFactory?.CreateLogger<LogSharkConfiguration>();
        }

        // Escape hatch for plugin configuration. Ideally these would be strongly typed too
        public IConfiguration GetPluginConfiguration(string pluginName)
        {
            return _config.GetSection($"PluginsConfiguration:{pluginName}");
        }

        public string AppendTo => _parameters.AppendTo;

        public bool ApplyPluginProvidedTagsToWorkbooks => _config.GetConfigurationValueOrDefault("TableauServer:ApplyPluginProvidedTagsToWorkbooks", true, _logger);

        public string CustomWorkbookSuffix
        {
            get
            {
                string returnValue = null;
                if (_parameters.WorkbookNameSuffixOverride != null)
                {
                    returnValue = _parameters.WorkbookNameSuffixOverride;
                }
                else if (_config.GetValue<bool>("EnvironmentConfig:AppendLogsetNameToOutput"))
                {
                    returnValue = "_" + Path.GetFileNameWithoutExtension(_parameters.LogSetLocation);
                }

                return returnValue;
            }
        }

        public string CustomWorkbookTemplatesDirectory => _config.GetValue<string>("EnvironmentConfig:CustomWorkbookTemplatesDir");

        public bool EnableNotifications => _config.GetValue<bool>("EnableNotifications");

        public bool ForceRunId => _parameters.ForceRunId;

        public List<string> GroupsToProvideWithDefaultPermissions => _config.GetSection("TableauServer:GroupsToProvideWithDefaultPermissions").Get<string[]>()?.ToList();

        public string FoghornTopicArn => _config.GetValue<string>("FoghornTopicArn");

        public string HyperLogDir => Path.GetDirectoryName(_config.GetValue<string>("Logging:PathFormat", "Logs/"));

        public string LogSetLocation => _parameters.LogSetLocation;

        public string MetricsNamespace => _config.GetValue<string>("MetricsNamespace");

        public string NotificationEmailSenderAddress => _config.GetValue<string>("NotificationSettings:EmailSenderAddress");

        public string NotificationEmailSenderDisplayName => _config.GetValue<string>("NotificationSettings:EmailSenderDisplayName");

        public string NotificationFallbackNotificationEmail => _config.GetValue<string>("NotificationSettings:FallbackNotificationEmail");

        public int NumberOfErrorDetailsToKeep => _config.GetValueAndThrowAtNull<int>("EnvironmentConfig:NumberOfErrorDetailsToKeep");

        public int NumberOfParallelThreads => _parameters.NumberOfParallelThreads ?? _config.GetConfigurationValueOrDefault("EnvironmentConfig:NumberOfProcessingThreads", 2, _logger);

        public string OriginalFileName => _parameters.OriginalFileName;

        public string OriginalLocation => _parameters.OriginalLocation;

        public string OutputDir => _config.GetValueAndThrowAtNull<string>("EnvironmentConfig:OutputDir");

        public int? OutputDirMaxResultsToKeep => _config.GetValue<int?>("EnvironmentConfig:OutputDirMaxResults");

        public string ParentProjectId => _config.GetStringWithDefaultIfEmpty("TableauServer:ParentProject:Id", null);

        public string ParentProjectName => _config.GetStringWithDefaultIfEmpty("TableauServer:ParentProject:Name", null);

        public ISet<string> PluginsToExcludeFromDefaultSet => _config.GetSemicolonSeparatedDistinctStringArray("PluginsConfiguration:DefaultPluginSet:PluginsToExcludeFromDefaultSet");

        public ISet<string> PluginsToRunByDefault => _config.GetSemicolonSeparatedDistinctStringArray("PluginsConfiguration:DefaultPluginSet:PluginsToRunByDefault");

        public int PostgresBatchSize => _config.GetValue<int>("PostgresWriterDatabase:BatchSize", 100);

        public string PostgresConnectionString => _parameters.DatabaseConnectionString ?? _config.GetValue<string>("PostgresWriterDatabase:ConnectionString");

        public int? PostgresConnectionTimeoutSeconds => _config.GetValue<int?>("PostgresWriterDatabase:ConnectionTimeoutSeconds");

        public string PostgresDatabaseName => _parameters.DatabaseName ?? _config.GetValue<string>("PostgresWriterDatabase:DatabaseName");

        public bool PostgresEmbedCredentialsOnPublish => _parameters.EmbedCredentialsOnPublish || _config.GetValue<bool>("PostgresWriterDatabase:EmbedCredentialsOnPublish");

        public string PostgresHost => _parameters.DatabaseHost ?? _config.GetValue<string>("PostgresWriterDatabase:Host");

        public string PostgresPassword => _parameters.DatabasePassword ?? _config.GetValue<string>("PostgresWriterDatabase:Password");

        public string PostgresPort => _parameters.DatabaseHost ?? _config.GetValue<string>("PostgresWriterDatabase:Port");

        public string PostgresServiceDatabaseName => _config.GetValue<string>("PostgresWriterDatabase:ServiceDatabaseName");
        
        public bool PostgresSkipDatabaseVerificationAndInitialization => _config.GetValue<bool>("PostgresWriterDatabase:SkipDatabaseVerificationAndInitialization");

        public string PostgresUsername => _parameters.DatabaseUsername ?? _config.GetValue<string>("PostgresWriterDatabase:Username");

        public bool PublishWorkbooks => _parameters.PublishWorkbooks;

        public string RequestedPlugins => _parameters.RequestedPlugins;

        public string RequestedWriter => _parameters.RequestedWriter ?? _config.GetValue<string>("EnvironmentConfig:DefaultWriter");

        public bool SkipPublishingEmptyWorkbooks => _config.GetConfigurationValueOrDefault("TableauServer:SkipPublishingEmptyWorkbooks", true, _logger);

        public string TableauServerPassword => _parameters.TableauServerPassword ?? _config.GetValueAndThrowAtNull<string>("TableauServer:Password");
        
        public string TableauServerProjectDescriptionFooterHtml => _parameters.TableauServerProjectDescriptionFooterHtml ?? _config.GetValue<string>("TableauServer:ProjectDescriptionFooterHtml");

        public string TableauServerSite => _parameters.TableauServerSite ?? _config.GetValueAndThrowAtNull<string>("TableauServer:Site");

        public int TableauServerTimeout => _config.GetValueAndThrowAtNull<int>("TableauServer:Timeout");
        public int TableauServerPublishingTimeout => _config.GetValueAndThrowAtNull<int>("TableauServer:PublishingTimeout");

        public string TableauServerUrl => _parameters.TableauServerUrl ?? _config.GetValueAndThrowAtNull<string>("TableauServer:Url");

        public string TableauServerUsername => _parameters.TableauServerUsername ?? _config.GetValueAndThrowAtNull<string>("TableauServer:Username");

        public string TelemetryApplication => _config.GetValue<string>("Telemetry:Application");

        public string TelemetryEndpoint => _config.GetValue<string>("Telemetry:Endpoint");

        public string TelemetryEnvironment => _config.GetValue<string>("Telemetry:Environment");

        public TelemetryLevel TelemetryLevel => _config.GetValue<TelemetryLevel?>("Telemetry:Level") ?? TelemetryLevel.None;

        public int TelemetryTimeout => _config.GetValue<int>("Telemetry:Timeout", 30);

        // Value needs to be cached because Path.GetTempPath is non-deterministic
        private string _tempDir;
        public string TempDir
        {
            get
            { 
                if (_tempDir == null)
                {
                    _tempDir = _config.GetStringWithDefaultIfEmpty("EnvironmentConfig:TempDirOverride", Path.Join(Path.GetTempPath(), "LogShark"));
                }
                return _tempDir;
            }
        }

        public bool ThrowIfOutputDirectoryExists => _config.GetConfigurationValueOrDefault("EnvironmentConfig:ThrowIfOutputDirectoryExists", true);
        public bool UsePluginsFromLogSharkAssembly => _config.GetConfigurationValueOrDefault("EnvironmentConfig:UsePluginsFromLogSharkAssembly", true);
        
        public string UserProvidedRunId => _parameters.UserProvidedRunId;

        public string WorkbookTemplatesDirectory => _config.GetValueAndThrowAtNull<string>("EnvironmentConfig:WorkbookTemplatesDir").FullyQualifyPathIfRelative(_rootDir);

        public string StarfishIssueUrl => _config.GetValueAndThrowAtNull<string>("Starfish:IssueUrl");

        public string StarfishArtifactUrl => _config.GetValueAndThrowAtNull<string>("Starfish:ArtifactUrl");

        public int StarfishPublishGbThreshold => _config.GetValueAndThrowAtNull<int>("Starfish:PublishGbThreshold");

        public bool PublishWorkbooksToStarfish => _config.GetValueAndThrowAtNull<bool>("Starfish:PublishWorkbooksToStarfish");
    }
}
