using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using log4net.Core;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.PluginInterfaces;
using Logshark.PluginLib.Model.Impl;
using Logshark.PluginModel.Model;
using Logshark.Plugins.ReplayCreation.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Logshark.Plugins.ReplayCreation
{
    /// <summary>
    /// Replay helps in replaying browser sessions using logs
    /// </summary>
    public class ReplayCreation : BasePlugin, IServerClassicPlugin, IServerTsmPlugin
    {
        public static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        //Regex that would break command namespace, command and arguments
        private const string CommandPatternFormat = "^(\\S+):(\\S+)\\s*(.*)";

        private static readonly Regex SCommandPatternRegex = new Regex(CommandPatternFormat);

        // if there are ':' then encode it with below String
        private const string SemiColonPreserver = "SEMICOLON";

        //date format as expected by the replay tool
        private const string UtcDateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        // time offset based on the timezone
        private TimeSpan _timeOffset;

        public override ISet<string> CollectionDependencies => new HashSet<string>
        {
            "httpd",
            "vizqlserver_cpp"
        };

        public ReplayCreation()
        {
        }

        public ReplayCreation(IPluginRequest request) : base(request)
        {
            //default log level for this plugin
            Log.Logger.Repository.Threshold = Level.Info;
        }

        /// <summary>
        /// Fix key format
        /// Replace all '-' and capitalize following letter
        /// </summary>
        /// <param name="keyStr"></param>
        /// <returns></returns>
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
        ///  Run regex to match keyvalue from a given string
        /// </summary>
        /// <param name="keyValPairs"></param>
        /// <returns></returns>
        private static Match MatchKeyValue(string keyValPairs)
        {
            var regexMatcher = Regex.Match(keyValPairs,
                   "([A-Za-z0-9-$-_-\"]+):((?:[\\{\\[\"].*|[^:]*))($|\\s|,)",
                   RegexOptions.IgnoreCase);
            return regexMatcher;
        }

        /// <summary>
        /// buildJson object from the keyValueString
        /// </summary>
        /// <param name="keyValueString">contains only the parameters of the command</param>
        /// <returns></returns>
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
        /// Get index where flower bracket ends, Strings within flower brackets on commands is properly formatted for JSON so
        /// we read them as it is with out any hack
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
        /// Get the string within the quotes
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static int GetStringQuotesIndex(string input)
        {
            var bQuoteStart = false;
            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '\"')
                {
                    if (bQuoteStart) // if we already saw quotes then this must be closing quote
                        return i;
                    else
                        bQuoteStart = true;
                }
            }

            // return invalid index
            return -1;
        }

        /// <summary>
        /// Deserialized the command string to Dictionary
        /// </summary>
        /// <param name="commandJsonString"></param>
        /// <returns></returns>
        private Dictionary<string, object> DeserializeToDictionary(string commandJsonString)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(commandJsonString);
            var values2 = new Dictionary<string, object>();
            foreach (var d in values)
            {
                //camel case the key value which is how tableau would take commands
                var key = CamelCaseKey(d.Key);
                if (d.Value is JObject)
                {
                    values2.Add(key, DeserializeToDictionary(d.Value.ToString()));
                }
                else
                {
                    values2.Add(key, d.Value);
                }
            }
            return values2;
        }

        /// <summary>
        /// Builds command to be run on browser using the JSON string This code has hack as the commands logged is not in
        /// proper JSON format, we need to first convert to proper JSON with commas between params and replace '=' with ':'
        /// etc and get the final command that can be run on the browser
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
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
        /// Queries and returns the vizqlsession ID list that corresponds to Apache session ID
        /// 
        /// </summary>
        /// <param name="browserSession">Browser session</param>
        /// <param name="accessRequestId">Request ID found in access logs</param>       
        /// <param name="mongoDatabase"></param>
        /// <returns></returns>
        private static List<string> GetVizqlSessionsForApacheRequestId(BrowserSession browserSession, string accessRequestId, IMongoDatabase mongoDatabase)
        {
            var vizqlSessionIDs = new List<string>();
            var vizqlSessionCollection = mongoDatabase.GetCollection<BsonDocument>("vizqlserver_cpp");
            var bUseLockSession = false; // flag to tell if lock session was used to get vizqlsession
            var vizqlSessionQuery = Query.Eq("k", "server-telemetry") & Query.Eq("v.request-info.rid", accessRequestId);
            var vizqlServerResults = vizqlSessionCollection.Find(vizqlSessionQuery).ToList();


            //if previous query did not result in finding a vizql session try looking for lock-session
            if (vizqlServerResults.Count == 0)
            {
                bUseLockSession = true;
                vizqlSessionQuery = Query.Eq("req", accessRequestId) & Query.Eq("k", "lock-session");
                vizqlServerResults = vizqlSessionCollection.Find(vizqlSessionQuery).ToList();
            }

            foreach (var vizqlServerLogLine in vizqlServerResults)
            {
                string vizqlSessionId;
                if (bUseLockSession)
                {
                    vizqlSessionId = (string)vizqlServerLogLine.GetValue("sess", null);
                }
                else
                {
                    vizqlSessionId = vizqlServerLogLine["v"]["sid"].ToString();
                }
                if (vizqlSessionId == null)
                {
                    //move to next vizqlserver line if this does not contain the vizqlserverSession
                    continue;
                }
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
                var bootStrapSessionQuery = Query.And(
                    Query.Eq("sess", vizqlSessionId), 
                    Query.Or(Query.Eq("k", "end-bootstrap-session"), Query.Eq("k", "end-bootstrap-session-action.bootstrap-session")), 
                    Query.Eq("v.new-session", "true"));
                var bootStrapSessionCollection = vizqlSessionCollection.Find(bootStrapSessionQuery).ToList();
                foreach (var bootStrapSession in bootStrapSessionCollection)
                {
                    var newAccessRequestId = bootStrapSession["v"]["new-session-id"].ToString();

                    if (!vizqlSessionIDs.Contains(newAccessRequestId))
                    {
                        vizqlSessionIDs.Add(newAccessRequestId);
                    }
                }
            }

            return vizqlSessionIDs;
        }

        ///  <summary>
        ///  Get commands for the apache session using the list of vizqlsessionIDs
        /// 
        ///  </summary>
        ///  <param name="browserSession">Browser session</param>
        /// <param name="mongoDatabase"></param>
        ///  <param name="vizqlSessionIds">vizqlSessionIDs used in case of correlated vizql sessions</param>
        ///  <returns></returns>
        private List<TabCommand> GetVizqlServerCommands(BrowserSession browserSession, IMongoDatabase mongoDatabase, List<string> vizqlSessionIds)
        {
            var commands = new List<TabCommand>();
            var vizqlSessionCollection = mongoDatabase.GetCollection<BsonDocument>("vizqlserver_cpp");
            foreach (var vizqlSessionId in vizqlSessionIds)
            {
                // making query compatible with old and newer command name, suggest to remove command-pre when tableau version < 2018.3 becomes less relevant
                var commandQuery = Query.And(Query.Regex("sess", vizqlSessionId + "*"), Query.Or(Query.Eq("k", "command-pre"), Query.Eq("k", "begin-commands-controller.invoke-command")));
                var commandsLogs = vizqlSessionCollection.Find(commandQuery).ToList();

                Log.DebugFormat("Getting commands for vizqlserver session {0}", vizqlSessionId);
                foreach (var commandLog in commandsLogs)
                {
                    //convert the current time to UTC
                    var browserStartTime = (DateTime)commandLog.GetElement("ts").Value - _timeOffset;
                    var timeStr = browserStartTime.ToUniversalTime().ToString(UtcDateFormat);

                    var jsonCmd = commandLog["v"]["args"].ToString();

                    var command = BuildCommand(jsonCmd);
                    if (command == null)
                    {
                        Log.WarnFormat("Failed to build command from-- {0}", jsonCmd);
                        continue;
                    }

                    //get the user name if it is not already there
                    if (browserSession.User == null)
                    {
                        var user = (string)commandLog.GetValue("user", null);
                        if (user != null)
                        {
                            browserSession.User = user;
                        }
                    }
                    var commandValue = new TabCommand(timeStr, command);
                    commands.Add(commandValue);
                }
            }

            //sort them as due to querying multiple vizqlsession the commands might come in different order
            commands.Sort();
            return commands;
        }

        /// <summary>
        /// Get the time offset from GMT using the access log entry
        /// </summary>
        /// <param name="bsonDocument"> Access log entry</param>
        /// <returns>Timespan value containing the offset</returns>
        private static TimeSpan GetTimeOffset(BsonDocument bsonDocument)
        {
            var offset = bsonDocument.GetElement("ts_offset").Value.ToString();
            var timeOffSet = int.Parse(offset);
            var ts = new TimeSpan(timeOffSet / 100, timeOffSet % 100, 0);
            return ts;
        }

        private new PluginResponse CreatePluginResponse()
        {
            return new PluginResponse(GetType().Name);
        }


        /// <summary>
        /// Process access log request by querying mongo DB
        /// </summary>
        /// <param name="accessRequest"></param>
        /// <param name="mongoDatabase"></param>
        /// <param name="browserSessions">List of browser sessions</param>
        /// <param name="mutexBrowserSessions"></param>
        /// <returns></returns>
        private BrowserSession ProcessAccessRequest(BsonDocument accessRequest, IMongoDatabase mongoDatabase, List<BrowserSession> browserSessions, Mutex mutexBrowserSessions)
        {
            AccessSessionInfo accessInfo;
            try
            {
                accessInfo = new AccessSessionInfo(accessRequest);
            }
            catch (Exception ex)
            {
                Log.DebugFormat(" Exception  {0}, moving on to next access request", ex.Message);
                return null;
            }
            var requestUrl = accessInfo.Resource;

            var urlParts = requestUrl.Split('/');
            var index = 0; //index from where search for "views" or "authoring" should start
            if (urlParts.Length > 2 && urlParts[1] == "t")
            {
                // there is site value so the index should be from 3
                // example in /t/MySite2/views/Election the value "views" is at index 3 with '/' being delimiter
                index = 3;
            }

            //process only the views or authoring requests
            for (var i = index; i < index + 2; i++)
            {
                if (urlParts[i] == "authoring" || urlParts[i] == "views")
                {
                    var browserSession = new BrowserSession();
                    browserSession.BrowserStartTime = accessInfo.RequestTime.ToString(UtcDateFormat);
                    browserSession.Url = requestUrl;
                    browserSession.AccessRequestId = accessInfo.ApacheRequestId;
                    browserSession.HttpStatus = accessInfo.StatusCode;
                    browserSession.LoadTime = accessInfo.LoadTime;

                    try
                    {
                        var vizqlSessionIDs = GetVizqlSessionsForApacheRequestId(browserSession, browserSession.AccessRequestId, mongoDatabase);
                        browserSession.Commands = GetVizqlServerCommands(browserSession, mongoDatabase, vizqlSessionIDs);
                    }
                    catch (Exception ex)
                    {
                        Log.DebugFormat(" Exception {0}, Exception encountered while parsing VizqlServer commands", ex.Message);
                        return null;
                    }

                    mutexBrowserSessions.WaitOne();
                    browserSessions.Add(browserSession);
                    // Release the Mutex.
                    mutexBrowserSessions.ReleaseMutex();

                    return browserSession;
                }
            }

            return null;
        }

        /// <summary>
        /// Execute function is called by Logshark framework to process the logs
        /// ReplayCreation plugin creates json file as output of the function
        /// </summary>
        /// <returns></returns>
        public override IPluginResponse Execute()
        {
            const string replayLocationKey = "ReplayCreation.ReplayLocation";
            const string replayFileNameKey = "ReplayCreation.ReplayFileName";
            var currentTime = DateTime.Now.ToString("_dd_MM_-HH-mm-ss");

            const string concurrentQueries = "ReplayCreation.ConcurrentQueries";
            var numConcurrentQueries = 5;

            //get number of concurrent requests
            try
            {
                var strNumConcurrentQueries = (string)pluginRequest.GetRequestArgument(concurrentQueries);
                numConcurrentQueries = int.Parse(strNumConcurrentQueries);
                Log.InfoFormat("Sending {0} concurrent queries to get browser sessions", numConcurrentQueries);
            }
            catch (KeyNotFoundException)
            {
                //ignore the exception and set the value to default concurrent requests
                Log.InfoFormat("Executing default {0} concurrent queries to get browser sessions", numConcurrentQueries);
            }

            //get path of the Json file from command line if specified
            string replayLocation;
            try
            {
                replayLocation = (string)pluginRequest.GetRequestArgument(replayLocationKey);
            }
            catch (KeyNotFoundException)
            {
                //ignore the exception and write file to default output directory
                replayLocation = pluginRequest.OutputDirectory;

                Log.InfoFormat("No path specified for Replay json file, it will be saved to default directory - {0}", replayLocation);
            }

            //get file name of the Json file from command line if specified
            string jsonReplayFile;
            try
            {
                jsonReplayFile = (string)pluginRequest.GetRequestArgument(replayFileNameKey);
            }
            catch (KeyNotFoundException)
            {
                //ignore the exception and get the file name using the current Time
                jsonReplayFile = "Playback" + currentTime + ".json";

                Log.InfoFormat("No file name specified for Replay json file, the replay file will be saved as - {0}", jsonReplayFile);
            }

            var fullFilePath = Path.Combine(replayLocation, jsonReplayFile);

            Log.Info("Executing ReplayCreation plugin");
            var response = CreatePluginResponse();

            var mongoDatabase = MongoDatabase;

            var browserSessions = new List<BrowserSession>();

            var collection = mongoDatabase.GetCollection<BsonDocument>("httpd");

            Log.Info("Querying Access logs for GET requests");
            var accessLogQuery = Query.Eq("request_method", "GET");

            var options = new FindOptions { NoCursorTimeout = true };

            var results = collection.Find(accessLogQuery, options).ToEnumerable();
            var accessResults = results as BsonDocument[] ?? results.ToArray();
            var numberOfAccessRequests = accessResults.Length;
            Log.InfoFormat("Number of Query Results : {0}", numberOfAccessRequests);

            //get the time offset from the first access log entry
            if (numberOfAccessRequests > 0)
            {
                _timeOffset = GetTimeOffset(accessResults.ElementAt(0));
            }
            
            var count = 1;
            var concurrentRunSemaphore = new Semaphore(numConcurrentQueries, numConcurrentQueries);

            var mutexBrowserSessions = new Mutex();
            var threadsActive = new CountdownEvent(1);

            const int intervalForVerbosePrint = 500;
            foreach (var result in accessResults)
            {
                //process multiple requests in parallel
                concurrentRunSemaphore.WaitOne();
                try
                {
                    threadsActive.AddCount();
                    new Thread(() =>
                    {
                        try
                        {
                            ProcessAccessRequest(result, mongoDatabase, browserSessions, mutexBrowserSessions);
                        }
                        catch (Exception ex)
                        {
                            Log.DebugFormat(" Exception {0} for url {1}", ex.Message, result.GetValue("resource").AsString);
                        }
                        finally
                        {
                            threadsActive.Signal();
                        }
                    }).Start();
                }
                finally
                {
                    concurrentRunSemaphore.Release();
                }
                count++;
                if ((count % intervalForVerbosePrint) == 0)
                {
                    //write log for every nth access request to see progress in console
                    Log.InfoFormat("Processed {0} access requests ", count);
                }
            }
            Log.Info("Wait for all requests to be processed");
            threadsActive.Signal();
            threadsActive.Wait();

            //serialize and write to json file
            Log.Info("Serializing Json replay sessions");
            var json = JsonConvert.SerializeObject(browserSessions, Formatting.Indented);

            Log.Info("Writing to Json file " + fullFilePath);
            File.WriteAllText(fullFilePath, json);

            return response;
        }
    }
}