using log4net.Config;
using Logshark.Common.Extensions;
using Logshark.Core;
using Logshark.RequestModel;
using Logshark.RequestModel.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Logshark.Tests.Runner
{
    public class LogsharkRunner
    {
        private readonly LogsharkConfiguration configuration;

        #region Public Methods

        public LogsharkRunner()
        {
            // Initialize log4net settings.
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(assemblyLocation));
            XmlConfigurator.Configure(new FileInfo(@"Config\Log.config"));

            configuration = LogsharkConfigReader.LoadConfiguration();
        }

        /// <summary>
        /// Sets up and issues the Request to the LogsharkController.
        /// </summary>
        /// <returns>0 if execution was successful; non-zero value otherwise.</returns>
        /// <param name="target">The location of the logset we want to test</param>
        /// <param name="postgresDatabaseName">The name of the postgres db we are creating</param>
        /// <param name="startLocalMongo">A bool to start a local instance of mongo or use whats defined in the app config</param>
        /// <param name="localMongoPort">The port that the local instance of mongo is running on (if being used)</param>
        /// <param name="forceParse">If we want to force parse the logset for the run</param>
        /// <param name="dropMongoDbPostRun">If we want to drop the mongo db post run</param>
        public LogsharkRunContext ProcessLogset(string target, string postgresDatabaseName, bool startLocalMongo = true, int localMongoPort = 27017, bool forceParse = true, bool dropMongoDbPostRun = false)
        {
            var pluginSet = new HashSet<string> { "none" };

            LogsharkRequest request = BuildLogsharkRequest(target, configuration, null, postgresDatabaseName, forceParse, startLocalMongo, localMongoPort, dropMongoDbPostRun, false, pluginSet, new List<string>(), Environment.CurrentDirectory);

            // Run application.
            LogsharkRequestProcessor requestProcessor = new LogsharkRequestProcessor();
            return requestProcessor.ProcessRequest(request);
        }

        #endregion Public Methods

        #region Private Methods

        private LogsharkRequest BuildLogsharkRequest(string target, LogsharkConfiguration configuration, string projectName, string postgresDatabaseName, bool forceParse, bool startLocalMongo, int localMongoPort,
                                                     bool dropMongoDbPostRun, bool publishWorkbooks, ISet<string> pluginsToExecute, IEnumerable<string> pluginCustomArguments, string currentWorkingDirectory)
        {
            if (String.IsNullOrWhiteSpace(target))
            {
                throw new ArgumentException("No logset target specified! Please pass in the correct location of your logset.");
            }

            // If the target is a relative path, we first need to convert it to an absolute path.
            if (!target.IsValidMD5() && !Path.IsPathRooted(target))
            {
                target = Path.Combine(currentWorkingDirectory, target);
            }

            return new LogsharkRequestBuilder(target, configuration)
                .WithProjectName(projectName)
                .WithPostgresDatabaseName(postgresDatabaseName)
                .WithForceParse(forceParse)
                .WithDropParsedLogset(dropMongoDbPostRun)
                .WithPublishWorkbooks(publishWorkbooks)
                .WithPluginsToExecute(pluginsToExecute)
                .WithPluginCustomArguments(ParseCommandLineArgToDictionary(pluginCustomArguments))
                .WithStartLocalMongo(startLocalMongo)
                .WithLocalMongoPort(localMongoPort)
                .GetRequest();
        }

        /// <summary>
        /// Parses a collection of command-line arg strings to a dictionary of key/value argument pairs.  String should be in format "argkey1:argval1 argkey2:argval2"
        /// </summary>
        /// <param name="args">A collection of args to parse.</param>
        /// <returns>Dictionary of parsed arguments.</returns>
        private IDictionary<string, object> ParseCommandLineArgToDictionary(IEnumerable<string> args)
        {
            var argCollection = new Dictionary<string, object>();

            foreach (var arg in args)
            {
                string[] keyAndValue = arg.Split(':');
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