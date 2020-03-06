using LogShark.LogParser.Containers;

namespace LogShark.Containers
{
    public class LogLine
    {
        public int LineNumber { get; }
        public LogFileInfo LogFileInfo { get; }
        public object LineContents { get; }

        public LogLine(ReadLogLineResult readLogLineResult, LogFileInfo logFileInfo)
        {
            LineNumber = readLogLineResult.LineNumber;
            LogFileInfo = logFileInfo;
            LineContents = readLogLineResult.LineContent;
        }
    }
}