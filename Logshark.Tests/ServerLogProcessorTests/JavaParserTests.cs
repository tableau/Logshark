using FluentAssertions;
using LogParsers.Base.Parsers;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.Parsers;
using Logshark.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Logshark.Tests.ServerLogProcessorTests
{
    [TestFixture, Description("Tests related to Java-format log parsing.")]
    public class JavaParserTests
    {
        [Test, Description("Parses a sample vizportal Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-28 18:12:56.918 -0700 (-,-,-,WfUrGAoapywAAAsIe-8AAABk,0:-28add3e7:15f616f30d4:-792a) catalina-exec-6 : INFO  com.tableausoftware.app.vizportal.LoggingInterceptor - Request received: /v1/recordNavigationTiming",
            ExpectedResult = @"{""ts"":""2017-10-28T11:12:56.918-07:00"",""ts_offset"":""-0700"",""req"":""WfUrGAoapywAAAsIe-8AAABk"",""local_req_id"":""0:-28add3e7:15f616f30d4:-792a"",""thread"":""catalina-exec-6"",""sev"":""INFO"",""class"":""com.tableausoftware.app.vizportal.LoggingInterceptor"",""message"":""Request received: /v1/recordNavigationTiming"",""line"":1}",
            Description = "10.4+")]
        [TestCase(@"2015-08-09 17:00:13.536 -0700 (Default,ppelland,a47h4G2463SVw8B8vWT7iOen2n7gAwR3,VcfpjAoRiGgAAUV0DMAAAAPn) catalina-exec-10 : INFO  com.tableausoftware.app.vizportal.LoggingInterceptor - Request completed: /v1/getUserAlertCount with status 200",
            ExpectedResult = @"{""ts"":""2015-08-09T10:00:13.536-07:00"",""ts_offset"":""-0700"",""site"":""Default"",""user"":""ppelland"",""sess"":""a47h4G2463SVw8B8vWT7iOen2n7gAwR3"",""req"":""VcfpjAoRiGgAAUV0DMAAAAPn"",""thread"":""catalina-exec-10"",""sev"":""INFO"",""class"":""com.tableausoftware.app.vizportal.LoggingInterceptor"",""message"":""Request completed: /v1/getUserAlertCount with status 200"",""line"":1}",
            Description = "9.0-10.3")]
        public string ParseVizportalLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new VizportalJavaParser());
        }

        [Test, Description("Parses a sample vizportal Java-format logfile into a collection of JSON documents.")]
        [TestCase("vizportal.log")]
        public void ParseVizportalFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new VizportalJavaParser());
        }

        [Test, Description("Parses a sample vizqlserver Java-format log line into a JSON document.")]
        public void ParseVizqlServerLogLine()
        {
            const string sampleLogLine =
                @"2015-04-17 11:50:10.150 -0700 (Default,local\E105682,4EB358A169A34FC89BA50780490672F0-0:0,VTFV4gongDYAADOYbQ0AAAJV) catalina-exec-6 : WARN  wgsessionId=QixCpAgrlBpT7rYgSCcB31daDLqXx99n org.hibernate.hql.internal.ast.HqlSqlWalker - [DEPRECATION] Encountered positional parameter near line 1, column 125.  Positional parameter are considered deprecated; use named parameters or JPA-style positional parameters instead.";
            const string expectedResult =
                @"{""ts"":""2015-04-17T04:50:10.15-07:00"",""ts_offset"":""-0700"",""site"":""Default"",""user"":""local\\E105682"",""sess"":""4EB358A169A34FC89BA50780490672F0-0:0"",""req"":""VTFV4gongDYAADOYbQ0AAAJV"",""thread"":""catalina-exec-6"",""sev"":""WARN"",""wgsession_id"":""QixCpAgrlBpT7rYgSCcB31daDLqXx99n"",""class"":""org.hibernate.hql.internal.ast.HqlSqlWalker"",""message"":""[DEPRECATION] Encountered positional parameter near line 1, column 125.  Positional parameter are considered deprecated; use named parameters or JPA-style positional parameters instead."",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new VizqlServerJavaParser());
        }

        [Test, Description("Parses a sample vizqlserver Java-format logfile into a collection of JSON documents.")]
        [TestCase("vizqlserver_java.log")]
        public void ParseVizqlServerFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new VizqlServerJavaParser());
        }

        [Test, Description("Parses a sample backgrounder Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-24 13:15:36.350 -0700 (Adam,,,1F2EAEAC32A24D96ADC9DCA8C562403E,1823635,:refresh_extracts,0:-28add3e7:15f616f30d4:-792a) pool-12-thread-4 : INFO  com.tableausoftware.domain.solr.SolrPendingQueueProcessor - Search index update sent. UpdateId [1836129] Index [DATASOURCE] Op [UPDATE] ObjectId [19871].",
            ExpectedResult = @"{""ts"":""2017-10-24T06:15:36.35-07:00"",""ts_offset"":""-0700"",""site"":""Adam"",""vql_sess_id"":""1F2EAEAC32A24D96ADC9DCA8C562403E"",""job_id"":""1823635"",""job_type"":""refresh_extracts"",""local_req_id"":""0:-28add3e7:15f616f30d4:-792a"",""thread"":""pool-12-thread-4"",""sev"":""INFO"",""class"":""com.tableausoftware.domain.solr.SolrPendingQueueProcessor"",""message"":""Search index update sent. UpdateId [1836129] Index [DATASOURCE] Op [UPDATE] ObjectId [19871]."",""line"":1}",
            Description = "10.5+, Local Request Id specified")]
        [TestCase(@"2017-10-24 13:15:36.350 -0700 (Adam,,,1F2EAEAC32A24D96ADC9DCA8C562403E,1823635,:refresh_extracts) pool-12-thread-4 : INFO  com.tableausoftware.domain.solr.SolrPendingQueueProcessor - Search index update sent. UpdateId [1836129] Index [DATASOURCE] Op [UPDATE] ObjectId [19871].",
            ExpectedResult = @"{""ts"":""2017-10-24T06:15:36.35-07:00"",""ts_offset"":""-0700"",""site"":""Adam"",""vql_sess_id"":""1F2EAEAC32A24D96ADC9DCA8C562403E"",""job_id"":""1823635"",""job_type"":""refresh_extracts"",""thread"":""pool-12-thread-4"",""sev"":""INFO"",""class"":""com.tableausoftware.domain.solr.SolrPendingQueueProcessor"",""message"":""Search index update sent. UpdateId [1836129] Index [DATASOURCE] Op [UPDATE] ObjectId [19871]."",""line"":1}",
            Description = "10.4")]
        [TestCase(@"2015-04-17 13:00:30.412 -0700 (,,,) backgroundJobRunnerScheduler-1 : INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: SUCCESS; name: List Extracts for TDFS Reaping; type :list_extracts_for_tdfs_reaping; notes: null; total time: 9 sec; run time: 0 sec",
            ExpectedResult = @"{""ts"":""2015-04-17T06:00:30.412-07:00"",""ts_offset"":""-0700"",""thread"":""backgroundJobRunnerScheduler-1"",""sev"":""INFO"",""class"":""com.tableausoftware.backgrounder.runner.BackgroundJobRunner"",""message"":""Job finished: SUCCESS; name: List Extracts for TDFS Reaping; type :list_extracts_for_tdfs_reaping; notes: null; total time: 9 sec; run time: 0 sec"",""line"":1}",
            Description = "9.0-10.3")]
        public string ParseBackgrounderLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new BackgrounderJavaParser());
        }

        [Test, Description("Parses a sample backgrounder Java-format logfile into a collection of JSON documents.")]
        [TestCase("backgrounder_java.log")]
        public void ParseBackgrounderFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new BackgrounderJavaParser());
        }

        [Test, Description("Parses a sample backuprestore Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-27 22:38:59.804 -0700 ClusterStateManager-ScheduledTask-0-EventThread : DEBUG com.tableausoftware.domain.solr.ClusterStateManager - Cluster state has been rebuilt.",
            ExpectedResult = @"{""ts"":""2017-10-27T15:38:59.804-07:00"",""ts_offset"":""-0700"",""thread"":""ClusterStateManager-ScheduledTask-0-EventThread"",""sev"":""DEBUG"",""class"":""com.tableausoftware.domain.solr.ClusterStateManager"",""message"":""Cluster state has been rebuilt."",""line"":1}",
            Description = "10.5+ sample line")]
        public string ParseBackupRestoreLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new BackupRestoreParser());
        }

        [Test, Description("Parses a sample databasemaintenance Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-28 00:43:30.100 -0700 db-op-0 : INFO  com.tableausoftware.db.maintenance.RolesAndDatabasesCreator - Updating user readonly in database.",
            ExpectedResult = @"{""ts"":""2017-10-27T17:43:30.1-07:00"",""ts_offset"":""-0700"",""thread"":""db-op-0"",""sev"":""INFO"",""class"":""com.tableausoftware.db.maintenance.RolesAndDatabasesCreator"",""message"":""Updating user readonly in database."",""line"":1}",
            Description = "10.5+ sample line")]
        public string ParseDatabaseMaintenanceLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new DatabaseMaintenanceParser());
        }

        [Test, Description("Parses a sample dataserver Java-format log line into a JSON document.")]
        public void ParseDataserverLogLine()
        {
            const string sampleLogLine =
                @"2015-08-15 04:04:09.725 -0700 (Default,jrepass,C461C7B715544089B0A1BA675F2113E5-1:0,Vc8cqQoRiGwAAE0If6IAAAPj) catalina-exec-36 : INFO  com.tableausoftware.controller.dataserver.SessionController - /dataserver/app/C461C7B715544089B0A1BA675F2113E5-1:0/progress.xml";
            const string expectedResult =
                @"{""ts"":""2015-08-14T21:04:09.725-07:00"",""ts_offset"":""-0700"",""site"":""Default"",""user"":""jrepass"",""sess"":""C461C7B715544089B0A1BA675F2113E5-1:0"",""req"":""Vc8cqQoRiGwAAE0If6IAAAPj"",""thread"":""catalina-exec-36"",""sev"":""INFO"",""class"":""com.tableausoftware.controller.dataserver.SessionController"",""message"":""/dataserver/app/C461C7B715544089B0A1BA675F2113E5-1:0/progress.xml"",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new DataServerJavaParser());
        }

        [Test, Description("Parses a sample filestore Java-format log line into a JSON document.")]
        public void ParseFileStoreLogLine()
        {
            const string sampleLogLine =
                @"2015-08-16 17:44:47.585 -0700 staleFoldersReaperScheduler-1   INFO  : com.tableausoftware.tdfs.filestore.FileReconciliationService - Reaped folderId 'allValidFolderIds3922318550630601865' of type 'allValidFolderIds";
            const string expectedResult =
                @"{""ts"":""2015-08-16T10:44:47.585-07:00"",""ts_offset"":""-0700"",""thread"":""staleFoldersReaperScheduler-1"",""sev"":""INFO"",""class"":""com.tableausoftware.tdfs.filestore.FileReconciliationService"",""message"":""Reaped folderId 'allValidFolderIds3922318550630601865' of type 'allValidFolderIds"",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new FileStoreParser());
        }

        [Test, Description("Parses a sample filestore Java-format logfile into a collection of JSON documents.")]
        [TestCase("filestore_java.log")]
        public void ParseFileStoreFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new FileStoreParser());
        }

        [Test, Description("Parses a sample gateway configuration Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-28 15:15:04.532 -0700 wrapper-1   INFO  : com.tableausoftware.gateway.HttpdConf - generating configuration...",
            ExpectedResult = @"{""ts"":""2017-10-28T08:15:04.532-07:00"",""ts_offset"":""-0700"",""thread"":""wrapper-1"",""sev"":""INFO"",""class"":""com.tableausoftware.gateway.HttpdConf"",""message"":""generating configuration..."",""line"":1}",
            Description = "10.5+ gateway configuration log line")]
        public string ParseHttpdConfigurationLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new HttpdConfigurationParser());
        }

        [Test, Description("Parses a sample search server Java-format log line into a JSON document.")]
        public void ParseSearchServerLogLine()
        {
            const string sampleLogLine =
                @"2015-08-16 17:00:15.100 -0700 (,,,) catalina-exec-346 : INFO  org.apache.solr.core.SolrCore - [unified_datasource] webapp=/solr path=/update params={update.distrib=FROMLEADER&distrib.from=http://10.17.136.105:11000/solr/unified_datasource/&wt=javabin&version=2} status=0 QTime=0";
            const string expectedResult =
                @"{""ts"":""2015-08-16T10:00:15.1-07:00"",""ts_offset"":""-0700"",""thread"":""catalina-exec-346"",""sev"":""INFO"",""class"":""org.apache.solr.core.SolrCore"",""message"":""[unified_datasource] webapp=/solr path=/update params={update.distrib=FROMLEADER&distrib.from=http://10.17.136.105:11000/solr/unified_datasource/&wt=javabin&version=2} status=0 QTime=0"",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new SearchServerParser());
        }

        [Test, Description("Parses a sample searchserver Java-format logfile into a collection of JSON documents.")]
        [TestCase("searchserver_java.log")]
        public void ParseSearchServerFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new SearchServerParser());
        }

        [Test, Description("Parses a sample tabadminagent Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-27 21:13:10.908 -0700 StatusRequestDispatcher-5 : ERROR com.tableausoftware.tabadmin.agent.status.ServiceStatusRequester - No JSON content in status response from vizportal_0. Raw data: [INFO] Loading configuration from /var/opt/tableau/tableau_server/data/tabsvc/services/vizportal_0.10500.17.1026.0800/bin/control-vizportal.runjavaservice.json",
            ExpectedResult = @"{""ts"":""2017-10-27T14:13:10.908-07:00"",""ts_offset"":""-0700"",""thread"":""StatusRequestDispatcher-5"",""sev"":""ERROR"",""class"":""com.tableausoftware.tabadmin.agent.status.ServiceStatusRequester"",""message"":""No JSON content in status response from vizportal_0. Raw data: [INFO] Loading configuration from /var/opt/tableau/tableau_server/data/tabsvc/services/vizportal_0.10500.17.1026.0800/bin/control-vizportal.runjavaservice.json"",""line"":1}",
            Description = "10.5+ sample line")]
        public string ParseTabAdminAgentLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new TabAdminAgentParser());
        }

        [Test, Description("Parses a sample tabadmincontroller Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-29 11:07:33.855 -0700 qtp1919349046-259 : INFO  com.tableausoftware.tabadmin.webapp.impl.linux.LinuxAuthenticationManager - User mmouse is authorized",
            ExpectedResult = @"{""ts"":""2017-10-29T04:07:33.855-07:00"",""ts_offset"":""-0700"",""thread"":""qtp1919349046-259"",""sev"":""INFO"",""class"":""com.tableausoftware.tabadmin.webapp.impl.linux.LinuxAuthenticationManager"",""message"":""User mmouse is authorized"",""line"":1}",
            Description = "10.5+ sample line")]
        public string ParseTabAdminControllerLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new TabAdminControllerJavaParser());
        }

        [Test, Description("Parses a sample tabsvc Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-27 21:06:31.916 -0700 scheduled-reap-ipc-1 : DEBUG com.tableausoftware.service.TabProcessRunner - Executing command [/var/opt/tableau/tableau_server/data/tabsvc/services/tabsvc_0.10500.17.1026.0800/tabsvc/reap_ipc, -v, -n]",
            ExpectedResult = @"{""ts"":""2017-10-27T14:06:31.916-07:00"",""ts_offset"":""-0700"",""thread"":""scheduled-reap-ipc-1"",""sev"":""DEBUG"",""class"":""com.tableausoftware.service.TabProcessRunner"",""message"":""Executing command [/var/opt/tableau/tableau_server/data/tabsvc/services/tabsvc_0.10500.17.1026.0800/tabsvc/reap_ipc, -v, -n]"",""line"":1}",
            Description = "10.5+ TSM-style sample line")]
        [TestCase(@"2015-05-19 05:23:34.349 +1000_WARN_server=:_service=:_session=_pid=_tid=:0x000030e8_logger=tabsvc.tabsvc_user=_session=_request=_message=Restarting dead component 'Tableau Server Backgrounder 0'.",
            ExpectedResult = @"{""ts"":""2015-05-18T22:23:34.349-07:00"",""ts_offset"":""+1000"",""sev"":""WARN"",""tid"":"":0x000030e8"",""logger"":""tabsvc.tabsvc"",""message"":""Restarting dead component 'Tableau Server Backgrounder 0'."",""line"":1}",
            Description = "9.0+ Classic-style sample line")]
        public string ParseTabSvcLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new TabSvcParser());
        }

        [Test, Description("Parses a sample clustercontroller Java-format log line into a JSON document.")]
        public void ParseClusterControllerLogLine()
        {
            const string sampleLogLine =
                @"2015-07-01 09:36:52.480 -0400 PathChildrenCache-3   INFO  : com.tableausoftware.cluster.postgres.WorkerNode - Current best node is CHPWPGTABP01NW with timestamp 1435757785892";
            const string expectedResult =
                @"{""ts"":""2015-07-01T02:36:52.48-07:00"",""ts_offset"":""-0400"",""thread"":""PathChildrenCache-3  "",""sev"":""INFO"",""class"":""com.tableausoftware.cluster.postgres.WorkerNode"",""message"":""Current best node is CHPWPGTABP01NW with timestamp 1435757785892"",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new ClusterControllerParser());
        }

        [Test, Description("Parses a sample clustercontroller Java-format logfile into a collection of JSON documents.")]
        [TestCase("clustercontroller_java.log")]
        public void ParseClusterControllerFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new ClusterControllerParser());
        }

        [Test, Description("Parses a sample service control Java-format log line into a JSON document.")]
        [TestCase(@"2017-10-27 22:19:40.366 -0700 main : ERROR com.tableausoftware.tabadmin.service.BaseTableauServiceCommands - Query status operation failed. Last error code: 0. Message: org.apache.thrift.transport.TTransportException: Error opening socket at '/tmp/tabsvc-backgrounder_8-thrift-e7b6caa1'.",
            ExpectedResult = @"{""ts"":""2017-10-27T15:19:40.366-07:00"",""ts_offset"":""-0700"",""thread"":""main"",""sev"":""ERROR"",""class"":""com.tableausoftware.tabadmin.service.BaseTableauServiceCommands"",""message"":""Query status operation failed. Last error code: 0. Message: org.apache.thrift.transport.TTransportException: Error opening socket at '/tmp/tabsvc-backgrounder_8-thrift-e7b6caa1'."",""line"":1}",
            Description = "10.5+ Error Line")]
        public string ParseServiceControlLogLine(string sampleLogLine)
        {
            return ParseJavaLogLine(sampleLogLine, new ServiceControlParser());
        }

        [Test, Description("Parses a sample tabadminservice Java-format log line into a JSON document.")]
        public void ParseTabAdminServiceLogLine()
        {
            const string sampleLogLine =
                @"2014-04-07 00:05:17.973 -0700 (,,,) main : INFO  com.tableausoftware.tabadmin.service.server.AppImpl - Loading config properties from URL file:C:/ProgramData/Tableau/Tableau Server/data/tabsvc/config/tabadminservice.properties";
            const string expectedResult =
                @"{""ts"":""2014-04-06T17:05:17.973-07:00"",""ts_offset"":""-0700"",""thread"":""main"",""sev"":""INFO"",""class"":""com.tableausoftware.tabadmin.service.server.AppImpl"",""message"":""Loading config properties from URL file:C:/ProgramData/Tableau/Tableau Server/data/tabsvc/config/tabadminservice.properties"",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new TabAdminServiceParser());
        }

        [Test, Description("Parses a sample tabadminservice Java-format logfile into a collection of JSON documents.")]
        [TestCase("tabadminservice_java.log")]
        public void ParseTabAdminServiceFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new TabAdminServiceParser());
        }

        [Test, Description("Parses a sample wgserver Java-format log line into a JSON document.")]
        public void ParseWgServerLogLine()
        {
            const string sampleLogLine =
                @"2015-08-16 17:30:41.510 -0700 pool-2-thread-4 Default  INFO  : com.tableausoftware.domain.solr.SolrPendingQueueProcessor - Search index update sent. UpdateId [2443320] Index [WORKBOOK] Op [UPDATE_CHILDREN] ObjectId [24328].";
            string expectedResult =
                @"{""ts"":""2015-08-16T10:30:41.51-07:00"",""ts_offset"":""-0700"",""thread"":""pool-2-thread-4"",""site"":""Default"",""sev"":""INFO"",""class"":""com.tableausoftware.domain.solr.SolrPendingQueueProcessor"",""message"":""Search index update sent. UpdateId [2443320] Index [WORKBOOK] Op [UPDATE_CHILDREN] ObjectId [24328]."",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new WgServerJavaParser());
        }

        [Test, Description("Parses a sample zookeeper Java-format log line into a JSON document.")]
        public void ParseZookeeperLogLine()
        {
            const string sampleLogLine =
                @"2015-07-01 09:36:30.354 -0400 (,,,) QuorumPeer[myid=1]/0:0:0:0:0:0:0:0:12000 : INFO  org.apache.zookeeper.server.NIOServerCnxn - Closed socket connection for client /3.47.61.224:52151 which had sessionid 0x14e4966503701d6";
            string expectedResult =
                @"{""ts"":""2015-07-01T02:36:30.354-07:00"",""ts_offset"":""-0400"",""thread"":""QuorumPeer[myid=1]/0:0:0:0:0:0:0:0:12000"",""sev"":""INFO"",""class"":""org.apache.zookeeper.server.NIOServerCnxn"",""message"":""Closed socket connection for client /3.47.61.224:52151 which had sessionid 0x14e4966503701d6"",""line"":1}";
            ParseJavaLogLine(sampleLogLine, expectedResult, new ZookeeperParser());
        }

        [Test, Description("Parses a sample zookeeper Java-format logfile into a collection of JSON documents.")]
        [TestCase("zookeeper_java.log")]
        public void ParseZookeeperFullFile(string logFile)
        {
            ParseFullJavaFile(logFile, new ZookeeperParser());
        }

        private static string ParseJavaLogLine(string logLine, IParser logParser)
        {
            return ParserTestHelpers.ParseSingleLine(logLine, logParser);
        }

        private static void ParseJavaLogLine(string logLine, string expectedResult, IParser logParser)
        {
            var actualResult = ParserTestHelpers.ParseSingleLine(logLine, logParser);
            Assert.AreEqual(expectedResult, actualResult);
        }

        private static void ParseFullJavaFile(string logFile, IParser parser, string sampleLogWorkerName = "worker0")
        {
            var logPath = TestDataHelper.GetServerLogProcessorResourcePath(logFile);

            IList<JObject> documents = ParserTestHelpers.ParseFile(logPath, parser, sampleLogWorkerName);

            // Count number of actual log events by counting the number of timestamps
            var lineCount = Regex.Matches(File.ReadAllText(logPath), @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{3} [+|-]\d{4}").Count;

            documents.Count.Should().Be(lineCount, "Number of parsed documents should match number of lines in file!");
        }
    }
}