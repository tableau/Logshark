using LogShark.Containers;
using LogShark.LogParser.Containers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogShark.Tests.Plugins.Helpers
{
    public class PluginTestCase
    {
        public object ExpectedOutput { get; set; }
        public int? LineNumber { get; set; }
        public object LogContents { get; set; }
        public LogFileInfo LogFileInfo { get; set; }
        public LogType LogType { get; set; }

        public LogLine GetLogLine()
        {
            return new LogLine(
                new ReadLogLineResult(LineNumber ?? 1, LogContents),
                LogFileInfo
            );
        }

        public static LogLine GetLogLineForExpectedOutput(int lineLumber, LogFileInfo logFileInfo)
        {
            return new LogLine(
                new ReadLogLineResult(lineLumber, null), // lineContent should not be used by Event object
                logFileInfo);
        }
    }
}