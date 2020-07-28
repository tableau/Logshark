using LogShark.LogParser.Containers;

namespace LogShark.LogParser
{
    public interface ILogTypeDetails
    {
        LogTypeInfo GetInfoForLogType(LogType logType);
    }
}