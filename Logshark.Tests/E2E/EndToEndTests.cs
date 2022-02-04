using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using FluentAssertions;
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

        private const string UnzipDir = "./TestData/EndToEndTests/logs_clean_tsm_unzipped";

        private const int ExpectedNumberOfLinesPersistedInTsmLogs = 59498; // Total lines LogShark writes when processing this set
        private const int ExpectedNumberOfLinesPersistedInTabadminLogs = 20068;

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
            if (Directory.Exists(UnzipDir))
            {
                Directory.Delete(UnzipDir, true);
            }

            ZipFile.ExtractToDirectory(TsmLogSetPath, UnzipDir);

            var logSharkParams = GetTestParameters(UnzipDir);
            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tsm_csv");
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTsmLogs);
        }
        
        [Fact]
        public async void Tabadmin_CSV_Unzipped()
        {
            if (Directory.Exists(UnzipDir))
            {
                Directory.Delete(UnzipDir, true);
            }

            ZipFile.ExtractToDirectory(TabadminLogSetPath, UnzipDir);

            var logSharkParams = GetTestParameters(UnzipDir);
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
            };

            var test = new EndToEndTest(logSharkParams, "./TestData/EndToEndTests/Expected/logs_clean_tabadmin_hyper", ExpectedTabadminProcessingErrors, configOverrides);
            await test.RunAndValidate(true, ExpectedNumberOfLinesPersistedInTabadminLogs);
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
