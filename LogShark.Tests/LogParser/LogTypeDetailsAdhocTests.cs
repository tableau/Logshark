using System.Linq;
using AwesomeAssertions;
using LogShark.Shared;
using LogShark.Shared.LogReading;
using Xunit;

namespace LogShark.Tests.LogParser
{
    // Verifies that, in adhoc mode, loose Tableau log files are classified to the correct LogType
    // purely by their filename (no folder structure).
    public class LogTypeDetailsAdhocTests : InvariantCultureTestsBase
    {
        private readonly LogTypeDetails _logTypeDetails = new LogTypeDetails(null);

        [Theory]
        [InlineData("nativeapi_vizqlserver_2-0_2024_01_01_00_00_00.txt", LogType.VizqlserverCpp)]
        [InlineData("nativeapi_backgrounder_2-1_2024_01_01_00_00_00.txt", LogType.BackgrounderCpp)]
        [InlineData("nativeapi_dataserver_1-0_2024_01_01_00_00_00.txt", LogType.DataserverCpp)]
        [InlineData("nativeapi_vizportal_1-0_2024_01_01_00_00_00.txt", LogType.VizportalCpp)]
        [InlineData("tabprotosrv_backgrounder_2-0.txt", LogType.ProtocolServer)]
        [InlineData("backgrounder_node1-0.log.2024-01-01", LogType.BackgrounderJava)]
        [InlineData("clustercontroller.log", LogType.ClusterController)]
        [InlineData("filestore.log", LogType.Filestore)]
        [InlineData("searchserver_node3-0.log", LogType.SearchServer)]
        [InlineData("appzookeeper_node1-0.log.2024-01-01", LogType.Zookeeper)]
        [InlineData("tabadmincontroller_node1-0.log", LogType.TabadminControllerJava)]
        [InlineData("tabadminagent_node1-0.log.2024-01-01", LogType.TabadminAgentJava)]
        [InlineData("access.2024_01_01_00_00_00.log", LogType.Apache)]
        [InlineData("postgresql-Mon.csv", LogType.PostgresCsv)]
        [InlineData("hyper_0_2024_01_01_15_07_26.log", LogType.Hyper)]
        [InlineData("netstat-anp.txt", LogType.NetstatLinux)]
        [InlineData("netstat-info.txt", LogType.NetstatWindows)]
        public void FlatFilenameClassifiesToExpectedLogType(string filename, LogType expectedLogType)
        {
            _logTypeDetails.GetInfoForLogType(expectedLogType)
                .FileBelongsToThisType(filename, adhocMode: true)
                .Should().BeTrue("'{0}' should be classified as {1} in adhoc mode", filename, expectedLogType);
        }

        [Theory]
        // A native (.txt) file must not be picked up by the Java (.log) matcher and vice versa
        [InlineData("nativeapi_backgrounder_2-1_2024_01_01_00_00_00.txt", LogType.BackgrounderJava)]
        [InlineData("backgrounder_node1-0.log.2024-01-01", LogType.BackgrounderCpp)]
        // A protocol file must not be mistaken for a native vizqlserver file
        [InlineData("tabprotosrv_vizqlserver_2-0.txt", LogType.VizqlserverCpp)]
        // Distinct process names must not cross-match
        [InlineData("vizportal_node1-0.log", LogType.VizdataJava)]
        public void FlatFilenameDoesNotMisclassify(string filename, LogType shouldNotMatch)
        {
            _logTypeDetails.GetInfoForLogType(shouldNotMatch)
                .FileBelongsToThisType(filename, adhocMode: true)
                .Should().BeFalse("'{0}' should NOT be classified as {1} in adhoc mode", filename, shouldNotMatch);
        }
    }
}
