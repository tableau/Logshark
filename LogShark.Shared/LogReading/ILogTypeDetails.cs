using LogShark.Shared.LogReading.Containers;

namespace LogShark.Shared.LogReading
{
    public interface ILogTypeDetails
    {
        LogTypeInfo GetInfoForLogType(LogType logType);
    }
}