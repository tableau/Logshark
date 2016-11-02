namespace Logshark.PluginLib.Logging
{
    internal static class LoggingConstants
    {
        internal static readonly string ConsolePattern = "%message%newline";
        internal static readonly string FilePattern = "%date %property{RunId} [%thread] %level %logger - %message%newline";

        internal static readonly int MaxFileSizeMb = 1;
        internal static readonly int MaxFileRollBackups = 10;
    }
}