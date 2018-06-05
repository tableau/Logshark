using CommandLine;
using log4net;
using log4net.Config;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Logshark.CLI
{
    internal class Program
    {
        private const string Log4NetConfigKey = "log4net-config-file";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code indicating whether program execution was successful.</returns>
        private static int Main(string[] args)
        {
            // Store CWD in case the executing assembly is being run from the system PATH.
            string currentWorkingDirectory = Environment.CurrentDirectory;

            // Initialize log4net settings.
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(assemblyLocation));
            try
            {
                XmlConfigurator.Configure(new FileInfo(ConfigurationManager.AppSettings[Log4NetConfigKey]));
            }
            catch (Exception ex)
            {
                Log.FatalFormat("Failed to initialize logging: {0}", ex.Message);
                return (int) ExitCode.InitializationError;
            }

            // Parse command line args.
            Log.DebugFormat("Logshark execution arguments: {0}", String.Join(" ", args));
            var options = new LogsharkCommandLineOptions();
            if (!Parser.Default.ParseArgumentsStrict(args, options, () => Log.Fatal("Unable to parse the provided arguments. Please check your syntax and try again.")))
            {
                // Parsing failed, exit with failure code.
                return (int) ExitCode.ArgumentParsingError;
            }

            // Execute!
            try
            {
                var logsharkCli = new LogsharkCLI(currentWorkingDirectory);
                return (int) logsharkCli.Execute(options);
            }
            catch (Exception)
            {
                return (int) ExitCode.ExecutionError;
            }
        }
    }
}