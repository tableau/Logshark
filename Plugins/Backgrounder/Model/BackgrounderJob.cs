using Logshark.PluginLib.Helpers;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using Tableau.ExtractApi.DataAttributes;

namespace Logshark.Plugins.Backgrounder.Model
{
    internal class BackgrounderJob
    {
        public long JobId { get; set; }
        public string JobType { get; set; }

        public int? Timeout { get; set; }
        public int Priority { get; set; }
        public bool Success { get; set; }
        public string Notes { get; set; }
        public string Args { get; set; }
        public int? TotalTime { get; set; }
        public int? RunTime { get; set; }
        public string WorkerId { get; set; }
        public int BackgrounderId { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public string StartFile { get; set; }
        public int StartLine { get; set; }
        public DateTime? EndTime { get; set; }
        public string EndFile { get; set; }
        public int? EndLine { get; set; }

        [ExtractIgnore]
        public BackgrounderJobDetail BackgrounderJobDetail { get; set; }

        [ExtractIgnore]
        public ICollection<BackgrounderJobError> Errors { get; set; }

        public BackgrounderJob()
        {
        }

        public BackgrounderJob(BsonDocument startEvent, BsonDocument finishEvent)
        {
            ParseStartEventElements(startEvent);
            ParseEndEventElements(finishEvent);
        }

        public BackgrounderJob(BsonDocument startEvent, bool timedOut)
        {
            if (timedOut)
            {
                ParseStartEventElements(startEvent);
                Success = false;
                EndTime = null;
                EndLine = null;
                ErrorMessage = "TimeoutExceptionReached";
            }
        }

        private void ParseStartEventElements(BsonDocument startEvent)
        {
            // Get Worker ID
            WorkerId = BsonDocumentHelper.GetString("worker", startEvent);

            // Get Backgrounder ID
            string fileName = BsonDocumentHelper.GetString("file", startEvent);
            int idStartIndex = fileName.IndexOf("-", StringComparison.InvariantCultureIgnoreCase) + 1;
            int idEndIndex = fileName.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
            string backgrounderIdString = fileName.Substring(idStartIndex, idEndIndex - idStartIndex);
            BackgrounderId = Int32.Parse(backgrounderIdString);

            // Get Start Time & log reference
            StartTime = BsonDocumentHelper.GetDateTime("ts", startEvent);
            StartFile = String.Format(@"{0}\{1}", BsonDocumentHelper.GetString("file_path", startEvent), fileName);
            StartLine = BsonDocumentHelper.GetInt("line", startEvent);

            // Get job id, if directly available
            if (startEvent.Contains("job_id"))
            {
                JobId = BsonDocumentHelper.GetLong("job_id", startEvent);
            }

            // Get job type, if directly available
            JobType = BsonDocumentHelper.GetString("job_type", startEvent);

            // Get elements off the message
            string message = BsonDocumentHelper.GetString("message", startEvent);
            IList<string> elements = message.Split(';');
            foreach (var item in elements)
            {
                if (String.IsNullOrWhiteSpace(JobType) && item.Contains("Running job of type"))
                {
                    string jobType = item.Split(new[] {':', ' '}).Last().Trim();
                    if (String.IsNullOrWhiteSpace(jobType))
                    {
                        throw new ArgumentOutOfRangeException(String.Format("Failed to parse job type from document '{0}'.", BsonDocumentHelper.GetString("_id", startEvent)));
                    }
                    JobType = jobType;
                }

                if (item.Contains("timeout:"))
                {
                    string timeout = item.Split(':').Last().Trim();
                    Timeout = Int32.Parse(timeout);
                }

                if (item.Contains("priority:"))
                {
                    string priority = item.Split(':').Last().Trim();
                    Priority = Int32.Parse(priority);
                }

                if (JobId == default(long) && item.Contains("id:"))
                {
                    string id = item.Split(':').Last().Trim();
                    long parsedJobId;
                    if (Int64.TryParse(id, out parsedJobId))
                    {
                        JobId = parsedJobId;
                    }
                }

                string argPrefix = "args:";
                if (item.Contains(argPrefix))
                {
                    int argStartIndex = item.LastIndexOf(argPrefix, StringComparison.InvariantCultureIgnoreCase) + argPrefix.Length;
                    string args = item.Substring(argStartIndex, item.Length - argPrefix.Length - 1).Trim();
                    if (!args.Equals("[]") && !args.Equals("null"))
                    {
                        Args = args;
                    }
                }
            }
        }

        private void ParseEndEventElements(BsonDocument endEvent)
        {
            // Get End Time & log reference
            EndTime = BsonDocumentHelper.GetDateTime("ts", endEvent);
            EndFile = String.Format(@"{0}\{1}", BsonDocumentHelper.GetString("file_path", endEvent), BsonDocumentHelper.GetString("file", endEvent));
            EndLine = BsonDocumentHelper.GetInt("line", endEvent);

            string severity = BsonDocumentHelper.GetString("sev", endEvent);

            if (severity.ToUpperInvariant().Equals("ERROR") || severity.ToUpperInvariant().Equals("FATAL"))
            {
                Success = false;
                ErrorMessage = BsonDocumentHelper.GetString("message", endEvent);
            }
            else
            {
                string message = BsonDocumentHelper.GetString("message", endEvent);
                IList<string> elements = message.Split(';');
                foreach (var item in elements)
                {
                    if (item.Contains("Job finished: SUCCESS"))
                    {
                        Success = true;
                    }

                    if (item.Contains("notes:"))
                    {
                        string notes = item.Split(':').Last().Trim();
                        if (!notes.Equals("null"))
                        {
                            Notes = notes;
                        }
                    }

                    if (item.Contains("total time:"))
                    {
                        string totalTime = item.Split(':')[1].Trim().Split(' ').First();
                        TotalTime = Int32.Parse(totalTime);
                    }

                    if (item.Contains("run time:"))
                    {
                        string runTime = item.Split(':')[1].Trim().Split(' ').First().Trim();
                        RunTime = Int32.Parse(runTime);
                    }
                }
            }
        }
    }
}