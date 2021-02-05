using System;
using LogShark.Extensions;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Plugins.VizqlDesktop.Model
{
    public class VizqlDesktopSession
    {
        public string CurrentWorkingDirectory { get; }
        public string Domain { get; }
        public string Hostname { get; }
        public string Os { get; }
        public int ProcessId { get; }    
        public DateTime StartTime { get; }
        public string TableauVersion { get; }
        public string SessionId { get; }

        public VizqlDesktopSession(NativeJsonLogsBaseEvent baseEvent)
        {
            var payloadJToken = baseEvent.EventPayload;
            var domain = payloadJToken.GetStringFromPath("domain");

            CurrentWorkingDirectory = payloadJToken.GetStringFromPath("cwd");
            Domain = domain == "''" ? null : domain;
            Hostname = payloadJToken.GetStringFromPath("hostname");
            Os = payloadJToken.GetStringFromPath("os");
            ProcessId = baseEvent.ProcessId;
            StartTime = baseEvent.Timestamp;
            TableauVersion = payloadJToken.GetStringFromPath("tableau-version");
            SessionId = $"{Hostname}_{ProcessId}_{StartTime:yyMMdd_HHmmssff}";
        }
    }
}