using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Containers;
using LogShark.Extensions;
using Logshark.Plugins.Replayer.Models;
using LogShark.Plugins.Shared;
using LogShark.Writers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogShark.Plugins.Replayer
{
    /// <summary>
    /// Replayer helps in replaying browser sessions using apache and vizqlserver logs
    /// </summary>
    public class ReplayerPlugin : IPlugin
    {
        private static ILogger<ReplayerPlugin> _logger;

        private static readonly List<LogType> ConsumedLogTypesStatic = new List<LogType> { LogType.Apache, LogType.VizqlserverCpp };

        private IProcessingNotificationsCollector _processingNotificationsCollector;

        public IList<LogType> ConsumedLogTypes => ConsumedLogTypesStatic;
        public string Name => "Replayer";

        //Regex that would break command namespace, command and arguments
        private const string CommandPatternFormat = "^(\\S+):(\\S+)\\s*(.*)";

        private static readonly Regex SCommandPatternRegex = new Regex(CommandPatternFormat, RegexOptions.Compiled);

        // if there are ':' then encode it with below String
        private const string SemiColonPreserver = "SEMICOLON";

        // Keys for plugin config values
        private const string ReplayerOutputDirectoryKey = "ReplayerOutputDirectory";
        private const string ReplayFileNameKey = "ReplayFileName";
        private const string ReplayRelevantEventsKey = "RelevantVizqlServerEvents";
        private const string ReplaySkippableCommandsKey = "SkippableCommands";
        private const string ReplayerTimeZonesDictionaryFileKey = "ReplayerTimeZonesDictionary";
        
        // members to be initialized in config section
        private string _replayerOutputDirectory = "ReplayerOutput";
        private string _jsonReplayFile = string.Empty;

        // lists of apache log lines
        private readonly List<Apache.ApacheEvent> _apacheEventCollection = new List<Apache.ApacheEvent>();

        // dictionary of vizqlserver session ids and list of (TimeStamp, command arguments, username)
        private Dictionary<string, List<Tuple<DateTime, string, string>>> _beginCommandController = new Dictionary<string, List<Tuple<DateTime, string, string>>>();

        // set of (RequestId, SessionId) pairs in lock-session events
        private Dictionary<string, List<string>> _lockSessionTuples = new Dictionary<string, List<string>>();

        // set of (v.request-info.rid, v.sid) pairs in server-telemetry events
        private Dictionary<string, List<string>> _serverTelemetryTuples = new Dictionary<string, List<string>>();

        // set of (SessionId, v.new-session) pairs in server-telemetry events
        private Dictionary<string, List<string>> _endBootstrapSessionTuples = new Dictionary<string, List<string>>();

        // time offset based on the timezone
        private TimeSpan _timeOffset;

        private readonly Dictionary<string, TimeSpan> _timeZoneDictionary = new Dictionary<string, TimeSpan>();

        private const string UtcDateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        // set of vizqlserver event types we care about
        private static HashSet<string> _relevantVizqlServerEvents = new HashSet<string>()
            {"server-telemetry","lock-session","end-bootstrap-session","end-bootstrap-session-action.bootstrap-session","command-pre","begin-commands-controller.invoke-command"};

        // set of vizqlserver commands we DO NOT need to replay as they are not user generated actions
        private static HashSet<string> _skippableCommands = new HashSet<string>()
            {"tabdoc:refresh-data-server", "tabdoc:get-world-update", "tabdoc:geographic-search-load-data", "tabdoc:highlight",
            "tabdoc:filter-targets", "tabdoc:navigate-to-sheet", "tabdoc:get-marks-color-uber-effects", "tabdoc:update-selection-delta",
            "tabdoc:get-flipboard-nav", "tabdoc:set-auto-update-server", "tabdoc:get-flipboard", "tabdoc:hit-test-scene",
            "tabdoc:show-detailed-error-dialog", "tabdoc:non-blocking-checkpoint-workbook-xml",
            "tabsrv:refresh-data-server", "tabsrv:get-world-update", "tabsrv:geographic-search-load-data", "tabsrv:highlight",
            "tabsrv:filter-targets", "tabsrv:navigate-to-sheet", "tabsrv:get-marks-color-uber-effects", "tabsrv:update-selection-delta",
            "tabsrv:get-flipboard-nav", "tabsrv:set-auto-update-server", "tabsrv:get-flipboard", "tabsrv:hit-test-scene",
            "tabsrv:show-detailed-error-dialog", "tabsrv:non-blocking-checkpoint-workbook-xml"};

        // file to read the time zones dictionary from
        private static string _timeZonesDictionaryFile = "/Resources/ReplayerTimeZonesDictionary.txt";

        public void Configure(IWriterFactory writerFactory, IConfiguration pluginConfig, IProcessingNotificationsCollector processingNotificationsCollector, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ReplayerPlugin>();
            _processingNotificationsCollector = processingNotificationsCollector;

            PopulateTimeZoneDictionary();
            CheckForMissingSystemTimeZones();

            // get the relevant vizqlserver events from config if specified
            _relevantVizqlServerEvents = pluginConfig?.GetConfigurationValueOrDefault(ReplayRelevantEventsKey, _relevantVizqlServerEvents.ToArray(), _logger)
                .ToHashSet();

            // get the relevant vizqlserver events from config if specified
            _skippableCommands = pluginConfig?.GetConfigurationValueOrDefault(ReplaySkippableCommandsKey, _skippableCommands.ToArray(), _logger)
                .ToHashSet();

            // get path of the Json file from config if specified
            _replayerOutputDirectory = pluginConfig?.GetConfigurationValueOrDefault(ReplayerOutputDirectoryKey, _replayerOutputDirectory, _logger);

            // get the file containing the time zones dictionary
            _timeZonesDictionaryFile = pluginConfig?.GetConfigurationValueOrDefault(ReplayerTimeZonesDictionaryFileKey, _timeZonesDictionaryFile, _logger);

            // get replay file name of the Json file from config if specified
            _jsonReplayFile = pluginConfig?.GetConfigurationValueOrDefault(ReplayFileNameKey, string.Format($"Playback{DateTime.Now:_dd_MM_-HH-mm-ss}.json"), _logger);
            if (string.IsNullOrEmpty(_jsonReplayFile))
            {
                _jsonReplayFile = string.Format($"Playback{DateTime.Now:_dd_MM_-HH-mm-ss}.json");
            }
        }

        public void ProcessLogLine(LogLine logLine, LogType logType)
        {
            switch (logType)
            {
                case LogType.Apache:
                    ProcessApacheLogLine(logLine);
                    break;
                case LogType.VizqlserverCpp:
                    ProcessVizqlServerCppLogLine(logLine);
                    break;
                default:
                    throw new ArgumentException($"{nameof(ReplayerPlugin)} does not accept '{logType}' log type");
            }
        }

        public SinglePluginExecutionResults CompleteProcessing()
        {
            _logger.LogInformation("Processing {0} apache source lines and {1} begin command controller, {2} server telemetry, {3} lock session, {4} end bootstrap vizqlserver events",
                _apacheEventCollection.Count, _beginCommandController.Count, _serverTelemetryTuples.Count, _lockSessionTuples.Count, _endBootstrapSessionTuples.Count);

            var browserSessions = new List<BrowserSession>();

            //get the time offset from the first access log entry
            if (_apacheEventCollection.Count > 0)
            {
                var firstOffset = _apacheEventCollection.First().TimestampOffset;
                _timeOffset = _timeZoneDictionary.GetValueOrDefault(firstOffset, new TimeSpan(0,0,0));
            }

            var count = 1;

            const int intervalForVerbosePrint = 5000;
            foreach (var result in _apacheEventCollection)
            {
                try
                {
                    browserSessions.Add(ProcessAccessRequest(result));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(" Exception {0} for url {1}", ex.Message, result.RequestBody);
                }
                count++;
                if ((count % intervalForVerbosePrint) == 0)
                {
                    //write log for every nth access request to see progress in console
                    _logger.LogInformation("Processed {0} access requests ", count);
                }
            }

            //sort, serialize and write to json file
            browserSessions.Sort((s, t) => string.CompareOrdinal(s.BrowserStartTime, t.BrowserStartTime));
            _logger.LogInformation("Serializing Json replay sessions");
            var json = JsonConvert.SerializeObject(browserSessions, Formatting.Indented);

            var fullFilePath = Path.Combine(_replayerOutputDirectory, _jsonReplayFile);
            _logger.LogInformation("Writing to Json file " + fullFilePath);

            // create the directory if it doesn't exist
            try
            {
                Directory.CreateDirectory(_replayerOutputDirectory);
            }
            catch (Exception e)
            {
                _processingNotificationsCollector.ReportError($"Exception {e.Message} while creating directory {_replayerOutputDirectory}.", _replayerOutputDirectory, 0, nameof(ReplayerPlugin));
            }

            try
            {
                File.WriteAllText(fullFilePath, json);
            }
            catch (Exception e)
            {
                _processingNotificationsCollector.ReportError($"Exception {e.Message} while writing file {fullFilePath}.", fullFilePath, 0, nameof(ReplayerPlugin));
            }


            return new SinglePluginExecutionResults(new List<WriterLineCounts>());
        }

        public void Dispose()
        {
        }

        private void ProcessApacheLogLine(LogLine logLine)
        {
            var apacheEvent = ApacheEventParser.ParseEvent(logLine);
            if (apacheEvent != null && apacheEvent.RequestMethod == "GET" && apacheEvent.StatusCode != 302 && IsVizLoadAccessEvent(apacheEvent))
            {
                _apacheEventCollection.Add(apacheEvent);
                if (_apacheEventCollection.Count % 10000 == 0)
                {
                    _logger.LogInformation("Added {0} Apache logs with GET request method", _apacheEventCollection.Count);
                }
            }
        }

        private void ProcessVizqlServerCppLogLine(LogLine logLine)
        {
            if (!(logLine.LineContents is NativeJsonLogsBaseEvent baseEvent))
            {
                var errorMessage = $"Was not able to cast line contents as {nameof(NativeJsonLogsBaseEvent)}";
                _logger.LogError(errorMessage);
                return;
            }

            if (_relevantVizqlServerEvents.Contains(baseEvent.EventType) && !_skippableCommands.Contains(baseEvent.EventPayload.GetStringFromPath("name")))
            {
                if (baseEvent.EventType == "server-telemetry")
                {
                    if (baseEvent.EventPayload.GetStringFromPath("request-info.action-type") != "bootstrap-session")
                    {
                        return;
                    }
                    string rid = baseEvent.EventPayload.GetStringFromPath("request-info.rid");
                    _serverTelemetryTuples = _serverTelemetryTuples.AddToDictionaryListOrCreate(rid, baseEvent.EventPayload.GetStringFromPath("sid"));
                    return;
                }

                if (baseEvent.EventType == "lock-session")
                {
                    _lockSessionTuples = _lockSessionTuples.AddToDictionaryListOrCreate(baseEvent.RequestId, baseEvent.SessionId);
                    return;
                }

                if (baseEvent.EventType == "end-bootstrap-session" || baseEvent.EventType == "end-bootstrap-session-action.bootstrap-session")
                {
                    if (baseEvent.EventPayload.GetStringFromPath("new-session") != "true")
                    {
                        return;
                    }
                    _endBootstrapSessionTuples = _endBootstrapSessionTuples.AddToDictionaryListOrCreate(baseEvent.SessionId, baseEvent.EventPayload.GetStringFromPath("new-session-id"));
                    return;
                }

                // extract the request ID, timestamp, command args and username
                _beginCommandController = _beginCommandController.AddToDictionaryListOrCreate(baseEvent.SessionId,new Tuple<DateTime, string, string>
                    (baseEvent.Timestamp, baseEvent.EventPayload.GetStringFromPath("args"), baseEvent.Username));
            }
        }

        /// <summary>
        /// Populates a dictionary with TimeSpan offsets for the timezone names.
        /// This is needed because GetSystemTimeZones works only with Standard time zones - not Daylight Saving.
        /// </summary>
        private void PopulateTimeZoneDictionary()
        {
            string timeZonesDictionaryFileLocation = $"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}/{_timeZonesDictionaryFile}";
            string[] timeZones = File.ReadAllLines(timeZonesDictionaryFileLocation);
            foreach (var tz in timeZones)
            {
                var kvp = tz.Split(',');
                _timeZoneDictionary.TryAdd(kvp[0], TimeSpan.Parse(kvp[1]));
            }
        }

        private void CheckForMissingSystemTimeZones()
        {
            var systemTimeZones = TimeZoneInfo.GetSystemTimeZones();
            foreach (var timeZone in systemTimeZones)
            {
                // check the standard time zone.
                if (!_timeZoneDictionary.Keys.Contains(timeZone.StandardName))
                {
                    _logger.LogWarning($"Provided time zone dictionary doesn't contain system time zone {timeZone.StandardName}.  Consider updating {_timeZonesDictionaryFile}");
                }

                // since the system timezone collection doesn't contain the daylight time zones, add them.
                if (!_timeZoneDictionary.Keys.Contains(timeZone.DaylightName))
                {
                    _logger.LogWarning($"Provided time zone dictionary doesn't contain system time zone {timeZone.DaylightName}.  Consider updating {_timeZonesDictionaryFile}");
                }
            }
        }

        /// <summary>
        /// Filters the access request with "views" and "authoring" in their request body.
        /// </summary>
        private bool IsVizLoadAccessEvent(Apache.ApacheEvent accessRequest)
        {
            var requestUrl = accessRequest.RequestBody;

            var urlParts = requestUrl.Split('/');
            var index = 0; //index from where search for "views" or "authoring" should start
            if (urlParts.Length > 2 && urlParts[1] == "t")
            {
                // there is site value so the index should be from 3
                // example in /t/MySite2/views/Election the value "views" is at index 3 with '/' being delimiter
                index = 3;
            }

            return urlParts.Skip(index).Take(2).Any(part => part == "authoring" || part == "views");
        }

        /// <summary>
        /// Process access log request by querying apache events
        /// </summary>
        private BrowserSession ProcessAccessRequest(Apache.ApacheEvent accessRequest)
        {
            var browserSession = new BrowserSession();
            var browserStartTime = accessRequest.Timestamp - _timeZoneDictionary[accessRequest.TimestampOffset];
            browserSession.BrowserStartTime = browserStartTime.ToString(UtcDateFormat);
            browserSession.Url = accessRequest.RequestBody;
            browserSession.AccessRequestId = accessRequest.RequestId;
            browserSession.HttpStatus = accessRequest.StatusCode.ToString();
            browserSession.LoadTime = accessRequest.RequestTimeMS.ToString();

            try
            {
                var vizqlSessionIDs = GetVizqlSessionsForApacheRequestId(browserSession, browserSession.AccessRequestId);
                browserSession.Commands = GetVizqlServerCommands(browserSession, vizqlSessionIDs);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(" Exception {0}, Exception encountered while parsing VizqlServer commands", ex.Message);
                return null;
            }

            return browserSession;
        }

        /// <summary>
        /// Queries and returns the vizqlsession ID list that corresponds to Apache session ID
        /// </summary>
        private List<string> GetVizqlSessionsForApacheRequestId(BrowserSession browserSession, string accessRequestId)
        {
            var vizqlSessionIDs = new List<string>();
            _serverTelemetryTuples.TryGetValue(accessRequestId, out var vizqlServerSessionCollection);

            //if previous query did not result in finding a vizql session try looking for lock-session
            if (vizqlServerSessionCollection == null || vizqlServerSessionCollection.Count == 0)
            {
                _lockSessionTuples.TryGetValue(accessRequestId, out vizqlServerSessionCollection);
            }

            if (vizqlServerSessionCollection == null || vizqlServerSessionCollection.Count == 0)
            {
                return vizqlSessionIDs;
            }

            foreach (var vizqlSessionId in vizqlServerSessionCollection)
            {
                if (vizqlSessionIDs.Contains(vizqlSessionId))
                {
                    //if we already looked at this vizqlSessionID then dont have to get the commands again
                    continue;
                }
                if (browserSession.VizqlSession == null)
                {
                    browserSession.VizqlSession = vizqlSessionId;
                }

                vizqlSessionIDs.Add(vizqlSessionId);

                //look for new bootstrap session
                if (_endBootstrapSessionTuples.ContainsKey(vizqlSessionId))
                {
                    var bootStrapSessionCollection = _endBootstrapSessionTuples[vizqlSessionId];
                    foreach (var bootStrapSession in bootStrapSessionCollection)
                    {
                        if (!vizqlSessionIDs.Contains(bootStrapSession))
                        {
                            vizqlSessionIDs.Add(bootStrapSession);
                        }
                    }
                }
            }

            return vizqlSessionIDs;
        }

        ///  <summary>
        ///  Get commands for the apache session using the list of vizqlsessionIDs
        ///  </summary>
        private List<TabCommand> GetVizqlServerCommands(BrowserSession browserSession, List<string> vizqlSessionIds)
        {
            var commands = new List<TabCommand>();
            foreach (var vizqlSessionId in vizqlSessionIds)
            {
                if (!_beginCommandController.ContainsKey(vizqlSessionId))
                {
                    continue;
                }

                var commandsLogs = _beginCommandController[vizqlSessionId];
                _logger.LogDebug("Getting commands for vizqlserver session {0}", vizqlSessionId);
                foreach (var (startTime, jsonCmd, user) in commandsLogs)
                {
                    //convert the current time to UTC
                    var browserStartTime = startTime - _timeOffset;
                    var browserStartTimeStr = browserStartTime.ToString(UtcDateFormat);

                    var command = BuildCommand(jsonCmd);
                    if (command == null)
                    {
                        _logger.LogWarning("Failed to build command from-- {0}", jsonCmd);
                        continue;
                    }

                    //get the user name if it is not already there
                    if (browserSession.User == null)
                    {
                        if (user != null)
                        {
                            browserSession.User = user;
                        }
                    }
                    var commandValue = new TabCommand(browserStartTimeStr, command);
                    commands.Add(commandValue);
                }
            }

            //sort them as due to querying multiple vizqlsession the commands might come in different order
            commands.Sort();
            return commands;
        }
        
        /// <summary>
        /// Builds command to be run on browser using the JSON string This code has hack as the commands logged is not in
        /// proper JSON format, we need to first convert to proper JSON with commas between params and replace '=' with ':'
        /// etc and get the final command that can be run on the browser
        /// TODO: Find a cleaner way to handle command parsing
        /// </summary>
        private Command BuildCommand(string jsonString)
        {
            // take the command first and parse the rest as Json
            var matcher = SCommandPatternRegex.Match(jsonString);
            if (!matcher.Success)
            {
                return null;
            }

            var commandNamespace = matcher.Groups[1].Value;
            var commandName = matcher.Groups[2].Value;
            var commandArg = matcher.Groups[3].Value;

            // if there are ':' lets preserve that to later replace it back
            commandArg = commandArg.Replace(":", SemiColonPreserver);
            commandArg = commandArg.Replace("=", ":");

            var cmdParams = BuildJson(commandArg);

            //create command object with command values
            var command = new Command(commandNamespace, commandName, cmdParams);

            return command;
        }

        /// <summary>
        /// buildJson object from the keyValueString
        /// TODO: Find a cleaner way to build json
        /// </summary>
        private Dictionary<string, object> BuildJson(string keyValueString)
        {
            var cmdParams = new Dictionary<string, object>();

            var regexMatcher = MatchKeyValue(keyValueString);

            while (regexMatcher.Success)
            {
                var key = CamelCaseKey(regexMatcher.Groups[1].Value);

                //matches string if the value starts with brackets '[' or '{'
                var regexBracketStringMatcher = Regex.Match(regexMatcher.Groups[2].Value, "^([\\{\\[\"].*)");
                if (regexBracketStringMatcher.Success)
                {
                    string val;
                    int restStartIndex;
                    if (regexMatcher.Groups[2].Value.StartsWith("\""))
                    {
                        var quotesEndIndex = GetStringQuotesIndex(regexBracketStringMatcher.Groups[1].Value);
                        restStartIndex = quotesEndIndex + 1;
                        val = regexBracketStringMatcher.Groups[1].Value.Substring(0, restStartIndex);

                    }
                    else
                    {
                        var bracketEndIndex = GetBracketEndIndex(regexBracketStringMatcher.Groups[1].Value);
                        restStartIndex = bracketEndIndex + 1;
                        val = regexBracketStringMatcher.Groups[1].Value.Substring(0, restStartIndex);
                    }
                    val = val.Replace(SemiColonPreserver, ":");
                    //get the rest of the command to parse it again
                    var rest = regexBracketStringMatcher.Groups[1].Value.Substring(restStartIndex);

                    //convert the subcommand from json to Dictionary
                    var subCmdParams = DeserializeToDictionary("{" + key + ":" + val + "}");

                    // add the dictionary value as key value pair
                    cmdParams.Add(key, subCmdParams[key]);

                    regexMatcher = MatchKeyValue(rest);
                }
                else
                {
                    var val = regexMatcher.Groups[2].Value;
                    cmdParams.Add(key, val);
                    regexMatcher = regexMatcher.NextMatch();
                }
            }
            return cmdParams;
        }

        /// <summary>
        ///  Run regex to match keyvalue from a given string
        /// </summary>
        private static Match MatchKeyValue(string keyValPairs)
        {
            var regexMatcher = Regex.Match(keyValPairs,
                   "([A-Za-z0-9-$-_-\"]+):((?:[\\{\\[\"].*|[^:]*))($|\\s|,)",
                   RegexOptions.IgnoreCase);
            return regexMatcher;
        }

        /// <summary>
        /// Fix key format
        /// Replace all '-' and capitalize following letter
        /// </summary>
        private static string CamelCaseKey(string keyStr)
        {
            // find all occurrences forward
            for (var i = -1; (i = keyStr.IndexOf('-', i + 1)) != -1;)
            {
                if (keyStr.Length - 1 > i + 1)
                {
                    var chAfter = keyStr[i + 1];
                    keyStr = keyStr.Substring(0, i) + char.ToUpper(chAfter) + keyStr.Substring(i + 2);
                }
            }
            return keyStr;
        }

        /// <summary>
        /// Get the string within the quotes
        /// </summary>
        private static int GetStringQuotesIndex(string input)
        {
            var bQuoteStart = false;
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '\"')
                {
                    if (bQuoteStart) // if we already saw quotes then this must be closing quote
                    {
                        return i;
                    }
                    
                    bQuoteStart = true;
                }
            }

            // return invalid index
            return -1;
        }

        /// <summary>
        /// Get index where flower bracket ends, Strings within flower brackets on commands is properly formatted for JSON so
        /// we read them as it is with out any hack
        /// </summary>
        private static int GetBracketEndIndex(string input)
        {
            var brackCnt = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '[' || input[i] == '{')
                {
                    brackCnt++;
                }
                else if (input[i] == ']' || input[i] == '}')
                {
                    brackCnt--;
                    if (brackCnt == 0)
                    {
                        return i;
                    }
                }
            }

            // return invalid index
            return -1;
        }

        /// <summary>
        /// Deserialized the command string to Dictionary
        /// </summary>
        private Dictionary<string, object> DeserializeToDictionary(string commandJsonString)
        {
            var cmdParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(commandJsonString);
            var kvp = new Dictionary<string, object>();
            foreach (var (dictKey, value) in cmdParams)
            {
                var key = CamelCaseKey(dictKey); //camel case the key value which is how tableau would take commands
                var valueToAdd = value is JObject
                    ? DeserializeToDictionary(value.ToString())
                    : value; 
                kvp.Add(key, valueToAdd);
            }
            return kvp;
        }
    }
}