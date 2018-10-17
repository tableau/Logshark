using log4net;
using Logshark.Common.Extensions;
using Logshark.Core;
using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.CLI
{
    /// <summary>
    /// Command-line wrapper for running Logshark.
    /// </summary>
    public sealed class LogsharkCLI
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly LogsharkConfiguration _configuration;
        private readonly string _currentWorkingDirectory;

        public LogsharkCLI(string currentWorkingDirectory)
        {
            _configuration = LogsharkConfigReader.LoadConfiguration();
            _currentWorkingDirectory = currentWorkingDirectory;
        }

        #region Public Methods

        /// <summary>
        /// Sets up and issues the <see cref="LogsharkRequest"/> to the <see cref="LogsharkRequestProcessor"/>.
        /// </summary>
        /// <returns>Exit code</returns>
        public ExitCode Execute(LogsharkCommandLineOptions commandLineOptions)
        {
            if (commandLineOptions.ListPlugins)
            {
                try
                {
                    LogsharkRequestProcessor.PrintAvailablePlugins();
                    return ExitCode.Success;
                }
                catch (Exception ex)
                {
                    Log.FatalFormat($"Unable to retrieve list of available plugins: {ex.Message}");
                    return ExitCode.ExecutionError;
                }
            }

            try
            {
                var request = BuildLogsharkRequest(commandLineOptions);

                var requestProcessor = new LogsharkRequestProcessor();
                var outcome = requestProcessor.ProcessRequest(request);

                return outcome.IsRunSuccessful.Equals(true) ? ExitCode.Success : ExitCode.ExecutionError;
            }
            catch (Exception ex)
            {
                Log.Debug(ex.GetFlattenedMessage());
                Log.Debug(ex.StackTrace);
                return ExitCode.ExecutionError;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private LogsharkRequest BuildLogsharkRequest(LogsharkCommandLineOptions commandLineArgs)
        {
            var target = commandLineArgs.Target;
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException("No logset target specified! See 'logshark --help' for usage examples.");
            }

            // If the target is a relative path, we first need to convert it to an absolute path.
            if (!target.IsValidMD5() && !Path.IsPathRooted(target))
            {
                target = Path.Combine(_currentWorkingDirectory, target);
            }

            try
            {
                var request = new LogsharkRequestBuilder(target, _configuration)
                    .WithCustomId(commandLineArgs.Id)
                    .WithDropParsedLogset(commandLineArgs.DropParsedLogset)
                    .WithForceParse(commandLineArgs.ForceParse)
                    .WithLocalMongoPort(commandLineArgs.LocalMongoPort)
                    .WithMetadata(ParseCommandLineArgToDictionary(commandLineArgs.Metadata))
                    .WithPluginCustomArguments(ParseCommandLineArgToDictionary(commandLineArgs.CustomArgs))
                    .WithPluginsToExecute(commandLineArgs.Plugins)
                    .WithPostgresDatabaseName(commandLineArgs.DatabaseName)
                    .WithProcessFullLogset(commandLineArgs.ParseAll)
                    .WithProjectDescription(commandLineArgs.ProjectDescription)
                    .WithProjectName(commandLineArgs.ProjectName)
                    .WithPublishWorkbooks(commandLineArgs.PublishWorkbooks)
                    .WithSiteName(commandLineArgs.SiteName)
                    .WithSource("CLI")
                    .WithStartLocalMongo(commandLineArgs.StartLocalMongo)
                    .WithWorkbookTags(commandLineArgs.WorkbookTags)
                    .GetRequest();

                return request;
            }
            catch (Exception ex)
            {
                Log.FatalFormat($"Invalid request: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parses a collection of command-line arg strings to a dictionary of key/value argument pairs.  String should be in format "argkey1:argval1 argkey2:argval2"
        /// </summary>
        /// <param name="args">A collection of args to parse.</param>
        /// <returns>Dictionary of parsed arguments.</returns>
        private static IDictionary<string, object> ParseCommandLineArgToDictionary(IEnumerable<string> args)
        {
            var argCollection = new Dictionary<string, object>();

            foreach (var arg in args)
            {
                var keyAndValue = arg.Split(':');
                if (keyAndValue.ToList().Count != 2)
                {
                    throw new ArgumentException("Invalid argument! Custom arguments must be formatted as ArgumentName:ArgumentValue", arg);
                }
                argCollection.Add(keyAndValue[0], keyAndValue[1]);
            }

            return argCollection;
        }

        #endregion Private Methods
    }
}