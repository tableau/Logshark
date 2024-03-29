﻿using System;

namespace LogShark.Plugins.Backgrounder.Model
{
    public class BackgrounderJob
    {
        public string Args { get; set; }
        public string BackgrounderId { get; set; }
        public string EndFile { get; set; }
        public int? EndLine { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; }
        public string JobId { get; set; }
        public string JobType { get; set; }
        public string Notes { get; set; }
        public int Priority { get; set; }
        public int? RunTime { get; set; }
        public string StartFile { get; set; }
        public int StartLine { get; set; }
        public DateTime StartTime { get; set; }
        public bool? Success { get; set; }
        public int? Timeout { get; set; }
        public int? TotalTime { get; set; }
        public string WorkerId { get; set; }

        public void AddInfoFromEndEvent(BackgrounderJob endEvent)
        {
            EndFile = endEvent.EndFile;
            EndLine = endEvent.EndLine;
            EndTime = endEvent.EndTime;
            ErrorMessage = endEvent.ErrorMessage;
            Notes = endEvent.Notes;
            RunTime = endEvent.RunTime;
            Success = endEvent.Success;
            TotalTime = endEvent.TotalTime;
        }

        public void MarkAsInvalidEnd()
        {
            Success = false;
            EndTime = null;
            EndLine = null;
            ErrorMessage = "There is no end event for this job in the logs, but a different job was processed later by the same backgrounder. This could be caused by a number of different problems: job was crashed, job was cancelled by other component, or log files are incomplete/corrupt";
        }

        public void MarkAsUnknown()
        {
            ErrorMessage = "Job end event seems to be outside of the time covered by logs";
        }
    }
}
