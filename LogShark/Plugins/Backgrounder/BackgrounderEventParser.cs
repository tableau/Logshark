using LogShark.Plugins.Backgrounder.Model;
using LogShark.Plugins.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Extensions;
using LogShark.Shared;
using LogShark.Shared.Extensions;
using LogShark.Shared.LogReading.Containers;
using System;

namespace LogShark.Plugins.Backgrounder
{
    public class BackgrounderEventParser
    {
        private readonly Dictionary<string, int?> _backgrounderIds;
        private readonly IBackgrounderEventPersister _backgrounderEventPersister;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        private readonly object _getBackgrounderIdLock;

        #region Regex

        private static readonly Regex BackgrounderIdFromFileNameRegex =
            new Regex(@"backgrounder(_node\d+)?-(?<backgrounder_id>\d+)\.",
                RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        //2018.2 linux/node1/backgrounder_0.20182.18.0627.22303567494456574693215/logs/backgrounder_node1-0.log:216:
        //2018-08-08 11:16:31.950 +1000 (,,,,1,:update_vertica_keychains,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Running job of type UpdateVerticaKeychains; no timeout; priority: 0; id: null; args: []
        //-----------ts---------- ts_of  ^^^^^--------job_type----------^   -------------thread-------------- --service---  sev-  ----------------------------class--------------------------   ------------------------------------------message--------------------------------------
        //                               |||||                          |>local_req_id
        //                               |||||>job_id
        //                               ||||>vql_sess_id
        //                               |||>data_sess_id
        //                               ||>user
        //                               |>site
        private static readonly Regex NewBackgrounderRegex =
            // 10.4+
            // 10.4 added "job type" and 10.5 added "local request id", either of which may be empty and thus are marked optional here
            // 2024.2 added "pid", marked optional to handle previous versions that do not have pid
            new Regex(@"^
                            (?<ts>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3})
                            \s(?<ts_offset>[^\s]+)
                            \s?(?<pid>[^\s]*)
                            \s\((?<site>[^,]*), (?<user>[^,]*), (?<data_sess_id>[^,]*), (?<vql_sess_id>[^,]*), (?<job_id>[^,]*), :?(?<job_type>[^,]*) ,(?<local_req_id>[^\s]*)\)
                            \s?(?<module>[^\s]*)?
                            \s(?<thread>[^\s]*)
                            \s(?<service>[^\s]*):
                            \s(?<sev>[A-Z]+)(\s+)
                            (?<class>[^\s]*)
                            \s?(?<callinfo>{.*})?
                            \s-\s(?<message>.*)",
               RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly object StartMessageRegexListLock = new object();
        private static readonly List<Regex> StartMessageRegexList = new List<Regex>() {
            // Running job of type UpdateVerticaKeychains; no timeout; priority: 0; id: null; args: []
            // --------------job_type_long---------------  -timeout--            ^      -id-        args
            //                                                                   |>priority         ^comma delimited inside [], or "null"
            new Regex(
                @"^Running\sjob\sof\stype\s(?<job_type_long>[^;]+);\s(?<timeout>[^;]+);\spriority:\s(?<priority>[0-9]+);\sid:\s(?<id>[^;]+);\sargs:\s(?<args>.+)$",
                RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled),


            new Regex(@"^activity=(?<activity>[^\s]*)\sjob_id=(?<job_id>[^\s]*)\sjob_type=(?<job_type_long>[^\s]*)\srequest_id=(?<request_id>[^\s]*)\s(args="")?(?<args>[^""]*)""?\s(user=)?(?<user>.*?)\s?(site="")?(?<site>[^""]*)?""?\s?(site_id=)?(?<site_id>[^\s]*)?\s?(guest=)?(?<guest>[^\s])?\s?(wg_session_id=)?(?<wg_session_id>.*?)?\s?timeout=(?<timeout>.*)$",
            RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled)
        };


        //2018.2 linux/node1/backgrounder_0.20182.18.0627.22303567494456574693215/logs/backgrounder_node1-0.log:219:
        //2018-08-08 11:16:32.173 +1000 (,,,,1,:update_vertica_keychains,-) scheduled-background-job-runner-1 backgrounder: INFO  com.tableausoftware.backgrounder.runner.BackgroundJobRunner - Job finished: SUCCESS;name: Update Vertica Keychains; type :update_vertica_keychains; id: 1; notes: null; total time: 601 sec; run time: 0 sec

        // Job finished: SUCCESS;name: Update Vertica Keychains; type :update_vertica_keychains; id: 1; notes: null; total time: 601 sec; run time: 0 sec
        // -----job_result------       ---------name-----------        ----------type----------      ^         notes             ^                  ^
        //                                                                                           |>id                        |>total_time       |>run_time
        private static readonly Regex EndMessageRegex =
            new Regex(
                @"(?<job_result>[^;]+);\s?name:\s(?<name>[^;]+);\s?type\s?:(?<type>[^;]+);\sid:\s(?<id>[^;]+);(\snotes:\s(?<notes>[^;]+);)?\stotal\stime:\s(?<total_time>[0-9]+)\ssec;\srun\stime:\s(?<run_time>[0-9]+)\ssec",
                RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        // Sending email from tableau@test.com to john.doe@test.com from server mail.test.com
        //                    --sender_email--    -recipient_email-             -smtp_server--
        private static readonly Regex SendEmailDetailsRegex =
            new Regex(
                @"Sending email from\s(?<sender_email>[^\s]*)\sto\s(?<recipient_email>[^\s]*)\sfrom server\s(?<smtp_server>.*)$",
                RegexOptions.ExplicitCapture | RegexOptions.Compiled); // Not ignoring pattern whitespace to keep regex cleaner

        // Starting Subscription Id 1071 for User test Created by test with Subject this is the subject
        //              -subscription_id-        -user-      -created_by_user-      -subscription_name-
        private static readonly Regex StartingSubscriptionRegex =
            new Regex(
                @"^
                    Starting\s[sS]ubscription\sId\s
                    (?<subscription_id>\d+)
                    \sfor\sUser\s
                    (?<user>[^\s]+)
                    (?:\sCreated\sby\s(?<created_by_user>[^\s]+))?
                    \s(?:with\s)?(?:Subject\s)?
                    \""?(?<subscription_name>.*?)\""?$",
                RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex FlowRunEventRegex = new Regex(@"(?<key>[a-zA-Z_]*)=(?<value>[\s\S\*]*?)(?=\|[a-zA-Z_]*=|$)", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex ExtractJobDetailsParseRegex = new Regex(@"\| (?<key>[a-zA-Z]+)=(?<value>[\s\S]*?)(?=\|[a-zA-Z]*=|$)",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        #endregion Regex

        public BackgrounderEventParser(IBackgrounderEventPersister backgrounderEventPersister, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _backgrounderIds = new Dictionary<string, int?>();
            _backgrounderEventPersister = backgrounderEventPersister;
            _processingNotificationsCollector = processingNotificationsCollector;

            _getBackgrounderIdLock = new object();
        }

        public void ParseAndPersistLine(LogLine logLine, string logLineText)
        {
            if (logLineText == null || logLine == null)
            {
                _processingNotificationsCollector.ReportError($"{nameof(BackgrounderEventParser)} received null log line or log string", logLine, nameof(BackgrounderPlugin));
                return;
            }

            var backgrounderId = GetBackgrounderId(logLine);

            var lineMatch = NewBackgrounderRegex.Match(logLineText);
            if (!lineMatch.Success)
            {
                _processingNotificationsCollector.ReportWarning($"Failed to process string as Backgrounder event. Likely an exception message", logLine, nameof(BackgrounderPlugin));
                return;
            }

            var errorEvent = ParseErrorMessage(lineMatch, logLine);
            if (errorEvent != null)
            {
                _backgrounderEventPersister.AddErrorEvent(errorEvent);
                return;
            }

            if (!int.TryParse(lineMatch.Groups["job_id"].Value, out var jobIdInt))
            {
                return; // We only allow error messages to not have job id
            }

            string jobId = jobIdInt.ToString();
            var startEvent = TryMatchStartMessage(lineMatch, logLine, backgrounderId.ToString(), jobId);
            if (startEvent != null)
            {
                _backgrounderEventPersister.AddStartEvent(startEvent);
                return;
            }

            var endEvent = TryMatchEndMessage(lineMatch, logLine, jobId);
            if (endEvent != null)
            {
                _backgrounderEventPersister.AddEndEvent(endEvent);
                return;
            }

            var extractJobDetails = TryMatchExtractJobDetails(lineMatch, jobId, logLine);
            if (extractJobDetails != null)
            {
                _backgrounderEventPersister.AddExtractJobDetails(extractJobDetails);
                return;
            }

            var subscriptionJobDetails = TryMatchSubscriptionJobDetails(lineMatch, jobId);
            if (subscriptionJobDetails != null)
            {
                _backgrounderEventPersister.AddSubscriptionJobDetails(subscriptionJobDetails);
                return;
            }
            var flowJobDetails = TryMatchFlowJobDetails(lineMatch, jobId, logLine);
            if (flowJobDetails != null)
            {
                _backgrounderEventPersister.AddFlowJobDetails(flowJobDetails);
                return;
            }
        }

        private int? GetBackgrounderId(LogLine logLine)
        {
            lock (_getBackgrounderIdLock)
            {
                if (_backgrounderIds.ContainsKey(logLine.LogFileInfo.FilePath))
                {
                    return _backgrounderIds[logLine.LogFileInfo.FilePath];
                }

                var backgrounderIdMatch = BackgrounderIdFromFileNameRegex.Match(logLine.LogFileInfo.FileName);
                var backgrounderIdValue = backgrounderIdMatch.Success &&
                                          int.TryParse(backgrounderIdMatch.Groups["backgrounder_id"].Value,
                                              out var parsedBackgrounderId)
                    ? parsedBackgrounderId
                    : (int?) null;

                if (backgrounderIdValue == null)
                {
                    const string message =
                        "Failed to parse backgrounderId from filename. All events from this file will have null id";
                    _processingNotificationsCollector.ReportError(message, logLine, nameof(BackgrounderPlugin));
                }

                _backgrounderIds.Add(logLine.LogFileInfo.FilePath, backgrounderIdValue);
                return backgrounderIdValue;
            }
        }

        private static BackgrounderJobError ParseErrorMessage(Match lineMatch, LogLine logLine)
        {
            if (lineMatch.Groups["sev"].Value != "ERROR" && lineMatch.Groups["sev"].Value != "FATAL")
            {
                return null;
            }

            long? backgrounderJobIdLong = long.TryParse(lineMatch.Groups["job_id"].Value, out var jobId) ? jobId : default(long?);
            return new BackgrounderJobError
            {
                BackgrounderJobId = backgrounderJobIdLong == null ? null : backgrounderJobIdLong.ToString(),
                Class = lineMatch.Groups["class"].Value,
                File = logLine.LogFileInfo.FileName,
                Line = logLine.LineNumber,
                Message = lineMatch.Groups["message"].Value,
                Severity = lineMatch.Groups["sev"].Value,
                Site = lineMatch.Groups["site"].Value,
                Thread = lineMatch.Groups["thread"].Value,
                Timestamp = TimestampParsers.ParseJavaLogsTimestamp(lineMatch.Groups["ts"].Value),
            };
        }

        private static BackgrounderJob TryMatchStartMessage(Match lineMatch, LogLine logLine, string backgrounderId, string jobId)
        {
            var message = lineMatch.Groups["message"].Value;
            var startMessageMatch = message?.GetRegexMatchAndMoveCorrectRegexUpFront(StartMessageRegexList, StartMessageRegexListLock);

            if (startMessageMatch == null || !startMessageMatch.Success)
            {
                return null;
            }

            var args = startMessageMatch.Groups["args"].Value;
            args = (args == "[]" || args == "null") ? null : args;
            var timeoutStr = startMessageMatch.Groups["timeout"].Value?.Replace("timeout: ", "");
            var timeout = int.TryParse(timeoutStr, out var to) ? to : default(int?);

            return new BackgrounderJob
            {
                Args = args,
                BackgrounderId = backgrounderId,
                JobId = jobId,
                JobType = lineMatch.Groups["job_type"].Value,
                Priority = int.TryParse(startMessageMatch.Groups["priority"].Value, out var pri) ? pri : default(int),
                StartFile = logLine.LogFileInfo.FileName,
                StartLine = logLine.LineNumber,
                StartTime = TimestampParsers.ParseJavaLogsTimestamp(lineMatch.Groups["ts"].Value),
                Timeout = timeout,
                WorkerId = logLine.LogFileInfo.Worker,
            };
        }

        private static BackgrounderJob TryMatchEndMessage(Match lineMatch, LogLine logLine, string jobId)
        {
            var message = lineMatch.Groups["message"].Value;
            if (!message.StartsWith("Job finished:") && !message.StartsWith("Error executing backgroundjob:"))
            {
                return null;
            }

            var endEvent = new BackgrounderJob
            {
                JobId = jobId,
                EndFile = logLine.LogFileInfo.FileName,
                EndLine = logLine.LineNumber,
                EndTime = TimestampParsers.ParseJavaLogsTimestamp(lineMatch.Groups["ts"].Value),
            };

            var endMessageMatch = EndMessageRegex.Match(message);
            var notes = endMessageMatch.Groups["notes"].Value;
            if (notes == "null" || string.IsNullOrWhiteSpace(notes))
            {
                notes = null;
            }

            if (message.StartsWith("Job finished: SUCCESS"))
            {
                endEvent.Success = true;
                endEvent.Notes = notes;
                endEvent.TotalTime = int.TryParse(endMessageMatch.Groups["total_time"].Value, out var totalTime) ? totalTime : default(int?);
                endEvent.RunTime = int.TryParse(endMessageMatch.Groups["run_time"].Value, out var runTime) ? runTime : default(int?);
            }
            else
            {
                endEvent.Success = false;
                endEvent.ErrorMessage = message;
            }

            return endEvent;
        }

        private BackgrounderExtractJobDetail TryMatchExtractJobDetails(Match lineMatch, string jobId, LogLine logLine)
        {
            var eventClass = lineMatch.Groups["class"].Value;
            var message = lineMatch.Groups["message"].Value;
            var timestamp = TimestampParsers.ParseJavaLogsTimestamp(lineMatch.Groups["ts"].Value);

            if (eventClass.Contains("VqlSessionService"))
            {
                return TryMatchOlderFormatOfExtractJobDetails(message, jobId, lineMatch.Groups["vql_sess_id"].Value, timestamp);
            } 
            else if (eventClass.Contains("RefreshExtractsWorker"))
                { 
            return TryMatchNewFormatOfExtractJobDetails(message, jobId, logLine, timestamp);
                }
            else { 

                    return null;
            
        }

        }

        //2019-04-30 06:16:53.626 -0400 (Enterprise Business Intelligence,,,3619ADAAA1B54302B9349F4368A3AAA3,4950695,:refresh_extracts,-) pool-19-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.service.VqlSessionService - Storing to SOS: OPPE/extract reducedDataId:9eb94068-5e1f-437b-9c4d-a1b700414ca8 size:134352 (twb) + 3604480 (guid={0F1B99B6-71A2-48C8-A9BC-7BA6D447515E}) = 3738832
        // Storing to SOS: OPPE/extract reducedDataId:9eb94068-5e1f-437b-9c4d-a1b700414ca8 size:134352 (twb) + 3604480 (guid={0F1B99B6-71A2-48C8-A9BC-7BA6D447515E}) = 3738832
        //                 extract_url-               -------------extract_id-------------      twb_size       extract_size   -----------extract_guid------------      total_size
        private static readonly Regex VqlSessionExtractDetailsRegex =
            new Regex(@"^
                        Storing\sto\s(repository|SOS):\s
                        (?<extract_url>.+?)/extract\s
                        (repoExtractId|reducedDataId):(?<extract_id>.+?)\s
                        size:(?<twb_size>\d+?)\s\(twb\)\s
                        \+\s(?<extract_size>\d+?)\s
                        \(guid={(?<extract_guid>[0-9A-F-]+?)}\)\s
                        =\s(?<total_size>\d+?)$",
                RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static BackgrounderExtractJobDetail TryMatchOlderFormatOfExtractJobDetails(string message, string jobId, string vizqlSessionId, DateTime timestamp)
        {
            var extractMatch = VqlSessionExtractDetailsRegex.Match(message);
            if (!extractMatch.Success)
            {
                return null;
            }

            return new BackgrounderExtractJobDetail
            {
                BackgrounderJobId = jobId,
                ExtractGuid = extractMatch.Groups["extract_guid"].Value,
                ExtractId = extractMatch.Groups["extract_id"].Value,
                ExtractSize = long.TryParse(extractMatch.Groups["extract_size"].Value, out var extractSize) ? extractSize : default(long?),
                ExtractUrl = extractMatch.Groups["extract_url"].Value,
                JobNotes = null, // Not available in the old format
                ScheduleName = null, // Not available in the old format
                Site = null, // Not available in the old format
                TotalSize = long.TryParse(extractMatch.Groups["total_size"].Value, out var totalSize) ? totalSize : default(long?),
                TwbSize = long.TryParse(extractMatch.Groups["twb_size"].Value, out var twbSize) ? twbSize : default(long?),
                VizqlSessionId = vizqlSessionId,
                Timestamp = timestamp
            };
        }


        // New format log example
        // 2019-08-09 21:50:17.641 +0000 (Default,,,,201,:refresh_extracts,ee6dd62e-f472-4252-a931-caf4dfb0009f) pool-12-thread-1 backgrounder: INFO  com.tableausoftware.model.workgroup.workers.RefreshExtractsWorker - |status=ExtractTimingSuccess|jobId=201|jobLuid=ee6dd62e-f472-4252-a931-caf4dfb0009f|siteName="Default"|workbookName="Large1"|refreshedAt="2019-08-09T21:50:17.638Z"|sessionId=F7162DFF82CB48D386850188BD5B190A-1:1|scheduleName="Weekday early mornings"|scheduleType="FullRefresh"|jobName="Refresh Extracts"|jobType="RefreshExtracts"|totalTimeSeconds=48|runTimeSeconds=46|queuedTime="2019-08-09T21:49:29.076Z"|startedTime="2019-08-09T21:49:31.262Z"|endTime="2019-08-09T21:50:17.638Z"|correlationId=65|priority=0|serialId=null|extractsSizeBytes=57016320|jobNotes="Finished refresh of extracts (new extract id:{78C1FCC2-E70E-4B25-BFFE-7B7F0096A4FE}) for Workbook 'Large1' "
        private static readonly Regex ExtractIdNewFormat = new Regex(
            @"\{(?<extractId>.*?)\}",
            RegexOptions.Compiled);
        private BackgrounderExtractJobDetail TryMatchNewFormatOfExtractJobDetails(string message, string jobId, LogLine logLine, DateTime timestamp)
        {
            if (!message.StartsWith('|'))
            {
                return null;
            }

            var messageParts = ExtractJobDetailsParseRegex.Matches(message).ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);

            var jobNotes = messageParts.GetStringValueOrNull("jobNotes").TrimSurroundingDoubleQuotes();
            var extractIdMatch = ExtractIdNewFormat.Match(jobNotes ?? string.Empty);
            var extractId = extractIdMatch.Success
                ? extractIdMatch.Groups["extractId"].Value
                : null;

            return new BackgrounderExtractJobDetail
            {
                BackgrounderJobId = jobId,
                ExtractGuid = null, // Not available in the new format
                ExtractId = extractId,
                ExtractSize = messageParts.GetLongValueOrNull("extractsSizeBytes"),
                ExtractUrl = messageParts.GetStringValueOrNull("workbookName").TrimSurroundingDoubleQuotes()
                             ?? messageParts.GetStringValueOrNull("datasourceName").TrimSurroundingDoubleQuotes(),
                JobNotes = jobNotes,
                ScheduleName = messageParts.GetStringValueOrNull("scheduleName").TrimSurroundingDoubleQuotes(),
                Site = messageParts.GetStringValueOrNull("siteName").TrimSurroundingDoubleQuotes(),
                TotalSize = null, // Not available in the new format
                TwbSize = null, // Not available in the new format
                VizqlSessionId = messageParts.GetStringValueOrNull("sessionId").TrimSurroundingDoubleQuotes(),
                status = messageParts.GetStringValueOrNull("status"),
                jobLuid = messageParts.GetStringValueOrNull("jobLuid"),
                jobType = messageParts.GetStringValueOrNull("jobType").TrimSurroundingDoubleQuotes(),
                jobName = messageParts.GetStringValueOrNull("jobName").TrimSurroundingDoubleQuotes(),
                refreshedAt = messageParts.GetStringValueOrNull("refreshedAt").TrimSurroundingDoubleQuotes(),
                scheduleName = messageParts.GetStringValueOrNull("scheduleName").TrimSurroundingDoubleQuotes(),
                scheduleType = messageParts.GetStringValueOrNull("scheduleType").TrimSurroundingDoubleQuotes(),
                totalTimeSeconds = messageParts.GetLongValueOrNull("totalTimeSeconds"),
                runTimeSeconds = messageParts.GetLongValueOrNull("runTimeSeconds"),
                startedTime = messageParts.GetStringValueOrNull("startedTime").TrimSurroundingDoubleQuotes(),
                queuedTime = messageParts.GetStringValueOrNull("queuedTime").TrimSurroundingDoubleQuotes(),
                endTime  = messageParts.GetStringValueOrNull("endTime").TrimSurroundingDoubleQuotes(),
                Timestamp = timestamp
            };
        }
        private BackgrounderFlowJobDetail TryMatchFlowJobDetails(Match lineMatch, string jobId, LogLine logLine)
        {
            var message = lineMatch.Groups["message"].Value;
            var @class = lineMatch.Groups["class"].Value;
            var timestamp = TimestampParsers.ParseJavaLogsTimestamp(lineMatch.Groups["ts"].Value);

            switch(@class)
            {
                case "com.tableausoftware.domain.content.flows.workers.RunFlowWorkerHelper" when message.Contains("FlowRunEvent") :
                    Dictionary<string, string> flowRun;
                    try
                    {
                        flowRun = FlowRunEventRegex.Matches(message).ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);
                    }
                    catch (ArgumentException ex) when (ex.Message.Contains("same key has already been added"))
                    {
                        _processingNotificationsCollector.ReportWarning($"Duplicate key found in FlowRunEvent message", logLine, nameof(BackgrounderPlugin));
                        // Use the fixed version that handles duplicates
                        flowRun = FlowRunEventRegex.Matches(message)
                            .GroupBy(m => m.Groups["key"].Value)
                            .ToDictionary(g => g.Key, g => g.Last().Groups["value"].Value);
                    }
                   
                    DateTime? queuedTime = null;
                    DateTime? startedTime = null;
                    DateTime? endTime = null;

                    var queuedTimeStr = flowRun.GetStringValueOrNull("queuedTime")?.TrimSurroundingDoubleQuotes();
                    var startedTimeStr = flowRun.GetStringValueOrNull("startedTime")?.TrimSurroundingDoubleQuotes();
                    var endTimeStr = flowRun.GetStringValueOrNull("endTime")?.TrimSurroundingDoubleQuotes();

                    try
                    {
                        if (!string.IsNullOrEmpty(queuedTimeStr) && queuedTimeStr != "NA")
                        {
                            queuedTime = TryParseDateTime(queuedTimeStr);
                        }

                        if (!string.IsNullOrEmpty(startedTimeStr) && startedTimeStr != "NA")
                        {
                            startedTime = TryParseDateTime(startedTimeStr);
                        }

                        if (!string.IsNullOrEmpty(endTimeStr) && endTimeStr != "NA")
                        {
                            endTime = TryParseDateTime(endTimeStr);
                        }
                    }
                    catch (Exception)
                    {
                        // If any DateTime parsing fails, keep the times as null
                        queuedTime = null;
                        startedTime = null;
                        endTime = null;
                    }

                    return new BackgrounderFlowJobDetail
                    {
                        BackgrounderJobId = jobId,
                        jobName = flowRun.GetStringValueOrNull("jobName")?.TrimSurroundingDoubleQuotes(),
                        status = flowRun.GetStringValueOrNull("status"),
                        flowId = flowRun.GetStringValueOrNull("flowId"),
                        queuedTime = queuedTime,
                        queueTimeSeconds = (flowRun.GetLongValueOrNull("queueTimeSeconds")),
                        startedTime = startedTime,
                        endTime = endTime,
                        totalTimeSeconds = flowRun.GetLongValueOrNull("totalTimeSeconds"),
                        runTimeSeconds = flowRun.GetLongValueOrNull("runTimeSeconds"),
                        runType = flowRun.GetStringValueOrNull("runType"),
                        flowName = flowRun.GetStringValueOrNull("flowName")?.TrimSurroundingDoubleQuotes(),
                        flowLuid = flowRun.GetStringValueOrNull("flowLuid")?.TrimSurroundingDoubleQuotes(),
                        outputStepIds = flowRun.GetStringValueOrNull("outputStepIds"),
                        rowsGenerated = flowRun.GetLongValueOrNull("rowsGenerated"),
                        flowRunMode = flowRun.GetStringValueOrNull("flowRunMode")?.TrimSurroundingDoubleQuotes(),
                        flow_run_error_type = flowRun.GetStringValueOrNull("flow_run_error_type"),
                        tableau_error_code = flowRun.GetStringValueOrNull("tableau_error_code"),
                        tableau_service_name = flowRun.GetStringValueOrNull("tableau_service_name")?.TrimSurroundingDoubleQuotes(),
                        tableau_status_code = flowRun.GetStringValueOrNull("tableau_status_code"),
                        errors = flowRun.GetStringValueOrNull("error"),
                        rootCauseErrorMsg = flowRun.GetStringValueOrNull("rootCauseErrorMsg")?.TrimSurroundingDoubleQuotes(),
                        Timestamp = timestamp
                    };
                    
                default:
                    return null;
            }
        }

        private static DateTime? TryParseDateTime(string dateTimeStr)
        {
            string[] formats = {
                "ddd MMM dd HH:mm:ss UTC yyyy",  // Wed Apr 03 03:00:27 UTC 2024
                "yyyy-MM-dd HH:mm:ss.fff"        // 2024-04-03 03:00:35.408
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateTimeStr, format, System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }
            
            return null;
        }

        private static BackgrounderSubscriptionJobDetail TryMatchSubscriptionJobDetails(Match lineMatch, string jobId)
        {
            var message = lineMatch.Groups["message"].Value;
            var @class = lineMatch.Groups["class"].Value;
            var timestamp = TimestampParsers.ParseJavaLogsTimestamp(lineMatch.Groups["ts"].Value);

            switch (@class)
            {
                case "com.tableausoftware.model.workgroup.service.VqlSessionService" when message.StartsWith("Created session id:"):
                    return new BackgrounderSubscriptionJobDetail
                    {
                        BackgrounderJobId = jobId,
                        VizqlSessionId = message.Split(':')[1],
                        Timestamp = timestamp
                    };

                case "com.tableausoftware.domain.subscription.SubscriptionRunner" when message.StartsWith("Starting subscription", System.StringComparison.InvariantCultureIgnoreCase):
                case "com.tableausoftware.model.workgroup.service.subscriptions.SubscriptionRunner" when message.StartsWith("Starting subscription", System.StringComparison.InvariantCultureIgnoreCase):
                    var subscriptionMatch = StartingSubscriptionRegex.Match(message);
                    return new BackgrounderSubscriptionJobDetail
                    {
                        BackgrounderJobId = jobId,
                        SubscriptionName = subscriptionMatch.Groups["subscription_name"].Value,
                        Timestamp = timestamp
                    };
                case "com.tableausoftware.model.workgroup.util.EmailHelper" when message.StartsWith("Sending email from"):
                    {
                        var emailMatch = SendEmailDetailsRegex.Match(message);
                        return new BackgrounderSubscriptionJobDetail
                        {
                            BackgrounderJobId = jobId,
                            SenderEmail = emailMatch.Groups["sender_email"].Value,
                            RecipientEmail = emailMatch.Groups["recipient_email"].Value,
                            SmtpServer = emailMatch.Groups["smtp_server"].Value,
                            Timestamp = timestamp
                        };
                    }
                default:
                    return null;
            }
        }
    }
}