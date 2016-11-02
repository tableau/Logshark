using System;
using System.IO;

namespace Logshark.PluginLib.Helpers
{
    internal static class LogFileHelper
    {
        internal static string GetLogDirectory()
        {
            return Path.Combine(AssemblyHelper.GetAssemblyDirectory(), "Logs");
        }

        internal static string GetLogFileName(string pluginName)
        {
            string logDirectory = GetLogDirectory();
            return String.Format(@"{0}{1}{2}.log.txt", logDirectory, Path.DirectorySeparatorChar, pluginName);
        }
    }
}