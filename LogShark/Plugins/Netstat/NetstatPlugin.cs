using System.Collections.Concurrent;
using LogShark.Containers;
using LogShark.Writers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Shared;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.Netstat
{
    public class NetstatPlugin : IPlugin
    {
        private readonly ConcurrentQueue<string> _skipRemainingInput = new ConcurrentQueue<string>();
        private IWriter<NetstatActiveConnection> _writer;
        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public IList<LogType> ConsumedLogTypes => new List<LogType> { LogType.NetstatLinux, LogType.NetstatWindows };
        public string Name => "Netstat";

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _writer = writerFactory.GetWriter<NetstatActiveConnection>(OutputInfo);
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            var writerStatistics = _writer.Close();
            return new SinglePluginExecutionResults(writerStatistics);
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            if (logType == LogType.NetstatLinux)
            {
                ParseLinuxLine(logLine);
            }
            else if (logType == LogType.NetstatWindows)
            {
                ParseWindowsLine(logLine);
            }
        }

        private void ParseLinuxLine(LogLine logLine)
        {
            var worker = logLine.LogFileInfo.Worker;

            if (_skipRemainingInput.Contains(worker))
            {
                return;
            }

            if (!(logLine.LineContents is string logString))
            {
                _processingNotificationsCollector.ReportError("Received null/non-string netstat data", logLine, nameof(NetstatPlugin));
                return;
            }

            // starts in Active Internet connections mode, then moves to Active UNIX domain sockets, which we dont care about
            if (logString.StartsWith("Active UNIX domain sockets"))
            {
                _skipRemainingInput.Enqueue(worker);
                return;
            }

            var match = NetstatLinuxInternetConnection.Match(logString);
            if (match == Match.Empty)
            {
                return;
            }
            
            var groups = match.Groups;
            var processName = groups["program_name"].Value;

            var parsedResult = new NetstatActiveConnection
            {
                FileLastModified = logLine.LogFileInfo.LastModifiedUtc,
                Line = logLine.LineNumber,
                Worker = worker,
                // Process names can be truncated in netstat output
                IsKnownTableauServerProcess = KnownTableauServerProcesses.Any(p => p.StartsWith(processName)),

                Protocol = groups["protocol"].Value,
                RecvQ = int.TryParse(groups["recv_q"].Value, out var rq) ? rq : (int?)null,
                SendQ = int.TryParse(groups["send_q"].Value, out var sq) ? sq : (int?)null,
                LocalAddress = groups["local_address"].Value,
                LocalPort = groups["local_port"].Value,
                ForeignAddress = groups["foreign_address"].Value,
                ForeignPort = groups["foreign_port"].Value,
                TcpState = groups["state"].Value,
                ProcessId = int.TryParse(groups["pid"].Value, out var pid) ? pid : (int?)null,
                ProcessName = processName,
            };

            _writer.AddLine(parsedResult);
        }

        private void ParseWindowsLine(LogLine logLine)
        {
            var worker = logLine.LogFileInfo.Worker;

            var connectionSection = logLine.LineContents as Stack<(string line, int lineNumber)>;

            if (connectionSection == null || connectionSection.Count == 0)
            {
                _processingNotificationsCollector.ReportError("Received null/unparsed netstat output", logLine, nameof(NetstatPlugin));
                return;
            }

            var processName = connectionSection.Pop().line.Trim(' ', '[', ']');
            var componentName = (string)null;
            var hasComponentName = !connectionSection.Peek().line.Contains(':');

            if (hasComponentName)
            {
                componentName = connectionSection.Peek().line.Trim();
            }

            foreach (var (line, lineNumber) in connectionSection)
            {
                var match = NetstatWindowsConnection.Match(line);
                if (match != Match.Empty)
                {
                    var groups = match.Groups;

                    var parsedResult = new NetstatActiveConnection
                    {
                        FileLastModified = logLine.LogFileInfo.LastModifiedUtc,
                        Line = lineNumber,
                        Worker = worker,
                        IsKnownTableauServerProcess = KnownTableauServerProcesses.Any(p => processName.StartsWith(p)),

                        ProcessName = processName,
                        ComponentName = componentName,

                        Protocol = groups["protocol"].Value,
                        LocalAddress = groups["local_address"].Value,
                        LocalPort = groups["local_port"].Value,
                        ForeignAddress = groups["foreign_address"].Value,
                        ForeignPort = groups["foreign_port"].Value,
                        TcpState = groups["state"].Success ? groups["state"].Value : null,
                    };

                    _writer.AddLine(parsedResult);
                }
            }
        }

        private static readonly DataSetInfo OutputInfo = new DataSetInfo("Netstat", "NetstatEntries");

        private static readonly ISet<string> KnownTableauServerProcesses = new HashSet<string>
        {
            "appzookeeper",
            "backgrounder",
            "clustercontroller",
            "dataserver",
            "filestore",
            "FNPLicensingService",
            "FNPLicensingService64",
            "httpd",
            "hyper",
            "hyperd",
            "lmgrd",
            "postgres",
            "redis-server",
            "searchserver",
            "tabadminagent",
            "tabadmincontroller",
            "tabadminservice",
            "tabadmwrk",
            "tabcmd",
            "tableau",
            "tabprotosrv",
            "tabrepo",
            "tdeserver",
            "tdeserver64",
            "vizportal",
            "vizqlserver",
            "wgserver",
            "zookeeper"
        };

        private static readonly Regex NetstatLinuxInternetConnection = new Regex(@"
                            ^
                            (?<protocol>\w+?)\s+
                            (?<recv_q>\d+?)\s+
                            (?<send_q>\d+?)\s+
                            (?<local_address>.+?):(?<local_port>[\d\*]+?)\s+
                            (?<foreign_address>.+?):(?<foreign_port>[\d\*]+?)\s+
                            ((?<state>\w+?)\s+)?
                            ((?<pid>\d+?)?/)?
                            (?<program_name>.+?)\s*$",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex NetstatWindowsConnection = new Regex(@"
                            ^\s+
                            (?<protocol>[A-Z]+)\s+
                            (?<local_address>[\[\]:.\w%*]+):(?<local_port>[\d*]+)\s+
                            (?<foreign_address>[\[\]:.\w%*]+):(?<foreign_port>[\d*]+)\s+
                            (?<state>\w+)?",
                    RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
    }
}