using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using AwesomeAssertions;
using LogShark.Containers;
using LogShark.Plugins.ClusterController;
using Xunit;

namespace LogShark.Tests.E2E
{
    public class EndToEndTests // These tests intentionally do not inherit from InvariantCultureTestBase, so they also verify that culture is set in LogSharkRunner
    {
        private const string EmptyZipPath = "./TestData/EndToEndTests/empty.zip";
        private const string PasswordProtectedZipPath = "./TestData/EndToEndTests/password1234.zip";
        private const string BadZip = "./TestData/EndToEndTests/notAzip.zip";
        private const string TsmLogSetPath = "./TestData/EndToEndTests/logs_clean_tsm.zip";
        private const string TabadminLogSetPath = "./TestData/EndToEndTests/logs_clean_tabadmin.zip";
        private const string TabadminLogSetPathApacheOnly = "./TestData/EndToEndTests/logs_clean_tabadmin_apache_only.zip";

        private const string UnzipDirTSM = "./TestData/EndToEndTests/logs_clean_tsm_unzipped";
        private const string UnzipDirTabadmin = "./TestData/EndToEndTests/logs_clean_tabadmin_unzipped";
        private const int ExpectedNumberOfLinesPersistedInTsmLogs = 60840; // Total lines LogShark writes when processing this set (includes appzookeeper)
        private const int ExpectedNumberOfLinesPersistedInTabadminLogs = 20043; // Includes appzookeeper

        private static readonly Dictionary<string, int> ExpectedTabadminProcessingErrors = new Dictionary<string, int>
        {
            {nameof(ClusterControllerPlugin), 1}
        };

        [Fact]
        public async void Empty_Zip()
        {
            var logSharkParams = GetTestParameters(EmptyZipPath);
            var test = new EndToEndTest(logSharkParams, null);
            var runSummary = await test.RunAndValidate(false);
            runSummary.ReasonForFailure.Should().Contain("Zip file appears to not contain any files");
        }
        
        [Fact]
        public async void Password_Protected_Zip()
        {
            var logSharkParams = GetTestParameters(PasswordProtectedZipPath);
            var test = new EndToEndTest(logSharkParams, null);
            var runSummary = await test.RunAndValidate(false);
            runSummary.ReasonForFailure.Should().Contain("unsupported compression method");
        }
        
        [Fact]
        public async void Bad_Zip()
        {
            var logSharkParams = GetTestParameters(BadZip);
            var test = new EndToEndTest(logSharkParams, null);
            var runSummary = await test.RunAndValidate(false);
            runSummary.ReasonForFailure.Should().Contain("does not exist or LogShark cannot open it");
        }
        
        [Fact]
        public async void Non_Existing_Zip()
        {
            var logSharkParams = GetTestParameters(BadZip + "do_not_exist.zip");
            var test = new EndToEndTest(logSharkParams, null);
            var runSummary = await test.RunAndValidate(false);
            runSummary.ReasonForFailure.Should().Contain("does not exist or LogShark cannot open it");
        }

        [Fact]
        public async void TSM_CSV_Zipped()
        {
            var logSharkParams = GetTestParameters(TsmLogSetPath);
            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tsm_csv");
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTsmLogs);
        }
        
        [Fact]
        public async void Tabadmin_CSV_Zipped()
        {
            var logSharkParams = GetTestParameters(TabadminLogSetPath);
            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tabadmin_csv", ExpectedTabadminProcessingErrors);
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTabadminLogs);
        }
        
        [Fact]
        public async void Tabadmin_CSV_Zipped_Apache_Only()
        {
            var logSharkParams = GetTestParameters(TabadminLogSetPathApacheOnly);
            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tabadmin_apache_only_csv");
            await test.RunAndValidate(true, 3343);
        }
        
        [Fact]
        public async void TSM_CSV_Unzipped()
        {
            if (Directory.Exists(UnzipDirTSM))
            {
                Directory.Delete(UnzipDirTSM, true);
            }

            ZipFile.ExtractToDirectory(TsmLogSetPath, UnzipDirTSM);

            var logSharkParams = GetTestParameters(UnzipDirTSM);
            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tsm_csv");
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTsmLogs);
        }
        
        [Fact]
        public async void Tabadmin_CSV_Unzipped()
        {
            if (Directory.Exists(UnzipDirTabadmin))
            {
                Directory.Delete(UnzipDirTabadmin, true);
            }

            ZipFile.ExtractToDirectory(TabadminLogSetPath, UnzipDirTabadmin);

            var logSharkParams = GetTestParameters(UnzipDirTabadmin);
            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tabadmin_csv", ExpectedTabadminProcessingErrors);
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTabadminLogs);
        }

        [Fact]
        public async void TSM_Hyper()
        {
            var logSharkParams = GetTestParameters(TsmLogSetPath, "hyper");
            var configOverrides = new Dictionary<string, string>()
            {
                ["EnvironmentConfig:AppendLogsetNameToOutput"] = "true",
                ["HyperProcess:external_stream_timeout"] = "3h",
            };
            logSharkParams.WorkbookNameSuffixOverride = "_test";

            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tsm_hyper", null, configOverrides);
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTsmLogs);
        }
        
        [Fact]
        public async void Tabadmin_Hyper()
        {
            var logSharkParams = GetTestParameters(TabadminLogSetPath, "hyper");
            var configOverrides = new Dictionary<string, string>()
            {
                ["EnvironmentConfig:AppendLogsetNameToOutput"] = "true",
                ["HyperProcess:external_stream_timeout"] = "3h",
            };

            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tabadmin_hyper", ExpectedTabadminProcessingErrors, configOverrides);
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTabadminLogs);
        }

        [SkippableFact]
        public async void Tabadmin_CSV_With_DynamicPlugin()
        {
            var samplePluginDir = FindSamplePluginOutputDir();
            Skip.If(samplePluginDir == null, "SampleApachePlugin has not been built. Run: dotnet publish DynamicPlugins/SampleApachePlugin -o DynamicPlugins/output/SampleApachePlugin");

            var logSharkParams = new LogSharkCommandLineParameters
            {
                AppendTo = null,
                UserProvidedRunId = null,
                LogSetLocation = TabadminLogSetPathApacheOnly,
                PublishWorkbooks = false,
                RequestedPlugins = "Apache;SampleApache",
                RequestedWriter = "csv",
                DynamicPluginsDirectory = samplePluginDir
            };

            var test = new EndToEndTest(logSharkParams, null, skipOutputAssert: true);
            var runSummary = await test.RunAndValidate(true);

            var writerStats = runSummary.ProcessLogSetResult.PluginsExecutionResults.GetWritersStatistics();
            writerStats.DataSets.Should().ContainKey(new DataSetInfo("Apache", "ApacheRequests"));
            writerStats.DataSets.Should().ContainKey(new DataSetInfo("SampleApache", "SampleApacheRequests"));
            writerStats.DataSets[new DataSetInfo("SampleApache", "SampleApacheRequests")].LinesPersisted.Should().BeGreaterThan(0);
        }

        private static string FindSamplePluginOutputDir()
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "DynamicPlugins", "output", "SampleApachePlugin");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "SampleApachePlugin.dll").Length > 0)
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static LogSharkCommandLineParameters GetTestParameters(string logSetLocation, string requestedWriter = "csv")
        {
            return new LogSharkCommandLineParameters
            {
                AppendTo = null,
                UserProvidedRunId = null,
                LogSetLocation = logSetLocation,
                PublishWorkbooks = false,
                RequestedPlugins = null,
                RequestedWriter = requestedWriter
            };
        }
    }
}
