using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Shared.LogReading.Containers;
using LogShark.Shared.LogReading.Readers;

namespace LogShark.Shared.LogReading
{
    public class LogTypeDetails : ILogTypeDetails
    {
        private readonly Dictionary<LogType, LogTypeInfo> _logFileInfoDictionary;

        public LogTypeDetails(IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _logFileInfoDictionary = LoadDetails(processingNotificationsCollector)
                .ToDictionary(logTypeInfo => logTypeInfo.LogType, logTypeInfo => logTypeInfo);
        }
        
        public LogTypeInfo GetInfoForLogType(LogType logType)
        {
            return _logFileInfoDictionary[logType];
        }

        public static IEnumerable<Regex> GetAllKnownLogFileLocations()
        {
            return LoadDetails(null) // null is fine here, as we never call factory methods than use this parameter
                .Where(logTypeInfo => logTypeInfo.LogType != LogType.CrashPackageLog &&
                                      logTypeInfo.LogType != LogType.CrashPackageManifest)
                .SelectMany(logTypeInfo => logTypeInfo.FileLocations);
        }

        private static IEnumerable<LogTypeInfo> LoadDetails(IProcessingNotificationsCollector processingNotificationsCollector)
        {
            return new List<LogTypeInfo>
            {
                new LogTypeInfo(
                    logType: LogType.Apache,
                    logReaderProvider: (stream, filePath) => new SimpleLinePerLineReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("httpd", "access"), // pre-TSM - httpd/access.*.log (access.2015_05_18_00_00_00.log)
                        TsmV0Log("httpd" , "access"), // TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\httpd\access.2018_08_08_00_00_00.log
                        TsmLog("gateway" , "access"), // TSM - node2\gateway_0.20182.18.0627.22308342643935754496180\logs\access.2018_08_08_00_00_00.log
                    }),

                new LogTypeInfo(
                    logType: LogType.BackgrounderCpp,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        TabadminNativeLog("backgrounder"), // pre-TSM - vizqlserver\Logs\backgrounder-0_2018_07_28_00_00_00.txt
                        TsmV0NativeLog("backgrounder"), // TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\backgrounder\nativeapi_backgrounder_1-0_2018_08_07_00_00_00.txt
                        TsmNativeLog("backgrounder"), // TSM - node2\backgrounder_0.20182.18.0627.22306436150448756480580\logs\nativeapi_backgrounder_2-1_2018_08_08_00_00_00.txt
                    }),

                new LogTypeInfo(
                    logType: LogType.BackgrounderJava,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("backgrounder"), // pre-TSM - backgrounder/backgrounder-*.log with optional date at the end (backgrounder-0.log.2015-05-18)
                        TsmV0Log("backgrounder"), // TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\backgrounder\backgrounder_node1-1.log
                        TsmLog("backgrounder"), // TSM - node2\backgrounder_0.20182.18.0627.22306436150448756480580\logs\backgrounder_node2-1.log.2018-08-08
                    }),

                new LogTypeInfo(
                    logType: LogType.ClusterController,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("clustercontroller"), // pre-TSM - clustercontroller/clustercontroller.log.2015-05-18
                        TsmV0Log("clustercontroller"), // TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\clustercontroller\clustercontroller.log
                        TsmLog("clustercontroller"), // TSM - node2\clustercontroller_0.20182.18.0627.22301467407848617992908\logs\clustercontroller.log
                    }),
                
                new LogTypeInfo(
                    logType: LogType.ControlLogsJava,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        // Control logs are placed in the logs folder of the component they control, so dir can be anything
                        TsmLog("[^/]+", "control"), // node1/tabadminagent_0.20202.20.0818.08576654979113587254208/logs/control_tabadminagent_node1-0.log.2020-09-27
                    }),
                
                new LogTypeInfo(
                    logType: LogType.CrashPackageLog,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"^[^/]*\.log$"),
                        Regex(@"^[^/]*\.txt$")
                    }),
                
                new LogTypeInfo(
                    logType: LogType.CrashPackageManifest,
                    logReaderProvider: (stream, filePath) => new SimpleLinePerLineReader(stream),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"^[^/]*\.manifest$")
                    }),
                
                new LogTypeInfo(
                    logType: LogType.DataserverCpp,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        TabadminNativeLog("dataserver"), // pre-TSM - vizqlserver\Logs\dataserver-0_2018_07_28_00_00_00.txt
                        TsmV0NativeLog("dataserver"), // TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\dataserver\nativeapi_dataserver_1-0_2018_08_07_00_00_00.txt
                        TsmNativeLog("dataserver"), // TSM - node2\dataserver_0.20182.18.0627.22301765668120146669553\logs\nativeapi_dataserver_2-1_2018_08_08_00_00_00.txt
                    }),
                
                new LogTypeInfo(
                    logType: LogType.DataserverJava,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("dataserver"),
                        TsmV0Log("dataserver"),
                        TsmLog("dataserver")
                    }),

                new LogTypeInfo(
                    logType: LogType.Filestore,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("filestore"), // pre-TSM - filestore/filestore.log.2018-07-31
                        TsmV0Log("filestore"), // TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\filestore\filestore.log
                        TsmLog("filestore"), // TSM - node1\filestore_0.20182.18.0627.22302895224363938766334\logs\filestore.log
                    }),
                
                new LogTypeInfo(
                    logType: LogType.Hyper,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("hyper"), // pre-TSM - hyper/hyper_2018_07_19_22_24_32.log
                        TsmV0Log("hyper"), // TSMv0 - localhost\tabadminagent_0.20181.18.0510.1418770265691097820228\logs\hyper\hyper_0_2018_07_30_08_08_24.log
                        TsmLog("hyper"), // TSM - node2\hyper_0.20182.18.0627.22308540150062437331610\logs\hyper_0_2018_08_08_15_07_26.log
                    }),
                
                new LogTypeInfo(
                    logType: LogType.NetstatLinux,
                    logReaderProvider: (stream, filePath) => new SimpleLinePerLineReader(stream),
                    fileLocations: new List<Regex>
                    {
                        Regex(@".+/netstat-anp\.txt$") // TSMv0 and TSM - (TSMv0 example: localhost\tabadminagent_0.20181.18.0510.1418770265691097820228\sysinfo\netstat-anp.txt)
                    }),
                
                new LogTypeInfo(
                    logType: LogType.NetstatWindows,
                    logReaderProvider: (stream, filePath) => new NetstatWindowsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"netstat-info\.txt$"), // pre-TSM - netstat-info.txt in the root
                        Regex(@"/netstat-info\.txt$") // TSM - node1\tabadminagent_0.20182.18.1001.21153436271280456730793\netstat-info.txt
                    }),
                
                new LogTypeInfo(
                    logType: LogType.PostgresCsv,
                    logReaderProvider: (stream, filePath) => new CsvLogReader<PostgresCsvMapping>(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("pgsql", "postgresql", "csv"), // pre-TSM - pgsql/postgresql-Sat.csv
                        TsmV0Log("pgsql", "postgresql", "csv"), // TSMv0 - localhost\tabadminagent_0.20181.18.0510.1418770265691097820228\logs\pgsql\postgresql-Mon.csv
                        TsmLog("pgsql", "postgresql", "csv"), // TSM - node2\pgsql_0.20182.18.0627.22303045353787439845635\logs\postgresql-Wed.csv
                    }),
                
                new LogTypeInfo(
                    logType: LogType.ProtocolServer,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"^tabprotosrv.*\.txt"), // Desktop, zipped files
                        Regex(@"^[Ll]ogs\tabprotosrv.*\.txt"), // Desktop, zipped Logs folder
                        TabadminLog("vizqlserver", "tabprotosrv", "txt"), // pre-TSM - vizqlserver\tabprotosrv_vizqlserver_0-0_1.txt
                        TsmV0ProtocolLog("backgrounder"), // TSMv0 - Backgrounder - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\backgrounder\tabprotosrv_backgrounder_1-0.txt
                        TsmV0ProtocolLog("dataserver"),   // TSMv0 - Dataserver 
                        TsmV0ProtocolLog("vizportal"),    // TSMv0 - Vizportal - localhost\tabadminagent_0.20181.18.0510.14183743094502915100234\logs\vizportal\tabprotosrv_vizportal_2-0.txt
                        TsmV0ProtocolLog("vizqlserver"),  // TSMv0 - Vizqlserver - locahost\tabadminagent_0.20181.18.0510.14183743094502915100234\logs\vizqlserver\tabprotosrv_vizqlserver_2-0.txt
                        TsmProtocolLog("backgrounder"), // TSM - Backgrounder - node2\backgrounder_0.20182.18.0627.22306436150448756480580\logs\tabprotosrv_backgrounder_2-0.txt
                        TsmProtocolLog("dataserver"),   // TSM - Dataserver
                        TsmProtocolLog("vizportal"),    // TSM - Vizportal - node1\vizportal_0.20182.18.0627.22304211226147125020104\logs\tabprotosrv_vizportal_1-0.txt
                        TsmProtocolLog("vizqlserver"),  // TSM - Vizqlserver - node1\vizqlserver_0.20182.18.0627.22305268092790927660381\logs\tabprotosrv_vizqlserver_1-1.txt
                    }),
                
                new LogTypeInfo(
                    logType: LogType.SearchServer,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("searchserver"), // pre-TSM - searchserver/searchserver-0.log.2018-07-19
                        TsmV0Log("searchserver"), // TSMv0 - localhost\tabadminagent_0.20181.18.0510.1418770265691097820228\logs\searchserver\searchserver_node1-0.log.2018-07-30
                        TsmLog("searchserver"), // TSM - node1\searchserver_0.20182.18.0627.22308531537836253176995\logs\searchserver_node1-0.log
                    }),

                new LogTypeInfo(
                    logType: LogType.Tabadmin,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"logs/tabadmin\.log"), // pre-TSM - logs/tabadmin.log
                        Regex(@"tabadmin/tabadmin.*\.log.*") // pre-TSM
                    }),
                
                new LogTypeInfo(
                    logType: LogType.TabadminAgentJava,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TsmLog("tabadminagent", "tabadminagent_node") // node1/tabadminagent_0.20202.20.0818.08576654979113587254208/logs/tabadminagent_node1-0.log.2020-09-26
                    }),
                
                new LogTypeInfo(
                    logType: LogType.TabadminControllerJava,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TsmLog("tabadmincontroller") // node1/tabadmincontroller_0.20201.20.0913.21097164108816806990865/logs/tabadmincontroller_node1-0.log
                    }),
                
                new LogTypeInfo(
                    logType: LogType.TabsvcYml,
                    logReaderProvider: (stream, filePath) => new YamlConfigLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"^tabsvc\.yml$"), // pre-TSM - tabsvc.yml in the root of the archive
                        // TSMv0 doesn't include config info
                        Regex(@"config/tabadminagent[^/]+/tabsvc\.yml$") // TSM - node1\tabadminagent_0.20182.18.1001.21153436271280456730793\config\tabadminagent_0.20182.18.1001.2115\tabsvc.yml
                    }),
                
                new LogTypeInfo(
                    logType: LogType.VizportalCpp,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        TabadminNativeLog("vizportal"), // pre-TSM - vizqlserver\Logs\vizportal_0-0_2018_08_01_00_00_00.txt
                        TsmV0NativeLog("vizportal"), // TSMv0 - localhost\tabadminagent_0.20181.18.0510.14183743094502915100234\logs\vizportal\nativeapi_vizportal_2-0_2018_07_31_00_00_00.txt
                        TsmNativeLog("vizportal"), // TSM - node1\vizportal_0.20182.18.0627.22304211226147125020104\logs\nativeapi_vizportal_1-0_2018_08_08_00_00_00.txt
                    }),
                
                new LogTypeInfo(
                    logType: LogType.VizportalJava,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("vizportal"), // pre-TSM - vizportal/vizportal-0.log.2018-07-22
                        TsmV0Log("vizportal"), // TSMv0 - localhost\tabadminagent_0.20181.18.0510.14183743094502915100234\logs\vizportal\vizportal_node2-0.log.2018-07-30
                        TsmLog("vizportal"), // TSM - node1\vizportal_0.20182.18.0627.22304211226147125020104\logs\vizportal_node1-0.log
                    }),

                new LogTypeInfo(
                    logType: LogType.VizqlserverCpp,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        TabadminNativeLog("vizqlserver"), // pre-TSM
                        TsmV0NativeLog("vizqlserver"), // TSMv0 - locahost\tabadminagent_0.20181.18.0510.14183743094502915100234\logs\vizqlserver\nativeapi_vizqlserver_2-1_2018_07_30_00_00_00.txt
                        TsmNativeLog("vizqlserver"), // TSM - node2\vizqlserver_0.20182.18.0627.22306727828712986214311\logs\nativeapi_vizqlserver_2-0_2018_08_08_00_00_00.txt
                    }),
                
                new LogTypeInfo(
                    logType: LogType.VizqlDesktop,
                    logReaderProvider: (stream, filePath) => new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"^log[^/]*\.txt"), // If somebody zipped files, log files are in root
                        Regex(@"^[Ll]ogs/log.*\.txt"), // If somebody zipped whole Logs folder
                    }),
                
                new LogTypeInfo(
                    logType: LogType.WorkgroupYml,
                    logReaderProvider: (stream, filePath) => new YamlConfigLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        Regex(@"config/workgroup\.yml$"), // pre-TSM & TSM. Pre TSM - config folder is at the root. TSM - node1\tabadminagent_0.20182.18.1001.21153436271280456730793\config\tabadminagent_0.20182.18.1001.2115\workgroup.yml
                        // TSMv0 doesn't include config info
                        Regex(@"config/tabadminagent[^/]+/workgroup\.yml$") // TSM - node1\tabadminagent_0.20182.18.1001.21153436271280456730793\config\tabadminagent_0.20182.18.1001.2115\workgroup.yml
                    }),
                
                new LogTypeInfo(
                    logType: LogType.Zookeeper,
                    logReaderProvider: (stream, _) => new MultilineJavaLogReader(stream),
                    fileLocations: new List<Regex>
                    {
                        TabadminLog("zookeeper"), // pre-TSM - zookeeper/zookeeper-0.log.2018-07-19
                        TsmV0Log("appzookeeper"),// TSMv0 - localhost\tabadminagent_0.20181.18.0404.16052600117725665315795\logs\appzookeeper\appzookeeper_node1-0.log
                        TsmLog("appzookeeper"), // TSM - node1\appzookeeper_1.20182.18.0627.22308155521326766729002\logs\appzookeeper_node1-0.log.2018-08-08
                    }),
            };
        }

        private static Regex Regex(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled);
        }

        private static Regex TabadminLog(string dir, string fileName = null, string extension = "log")
        {
            const string pattern = @"{0}/{1}.*\.{2}";
            return Regex(string.Format(pattern, dir, fileName ?? dir, extension));
        }

        private static Regex TabadminNativeLog(string name)
        {
            return TabadminLog("vizqlserver/Logs", name, "txt");
        }
        
        private static Regex TsmV0Log(string dir, string fileName = null, string extension = "log")
        {
            const string pattern = @"[^/]+/tabadminagent[^/]*/logs/{0}/{1}.*\.{2}";
            return Regex(string.Format(pattern, dir, fileName ?? dir, extension));
        }

        private static Regex TsmV0NativeLog(string name)
        {
            return TsmV0Log(name, "nativeapi_" + name, "txt");
        }
        
        private static Regex TsmV0ProtocolLog(string name)
        {
            return TsmV0Log(name, "tabprotosrv_" + name, "txt");
        }

        private static Regex TsmLog(string dir, string fileName = null, string extension = "log")
        {
            const string pattern = @"/{0}[^/]*/logs/{1}.*\.{2}";
            return Regex(string.Format(pattern, dir, fileName ?? dir, extension));
        }
        
        private static Regex TsmNativeLog(string name)
        {
            return TsmLog(name, "nativeapi_" + name, "txt");
        }
        
        private static Regex TsmProtocolLog(string name)
        {
            return TsmLog(name, "tabprotosrv_" + name, "txt");
        }
    }
}