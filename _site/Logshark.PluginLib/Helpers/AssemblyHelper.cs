using System;
using System.IO;
using System.Reflection;

namespace Logshark.PluginLib.Helpers
{
    internal static class AssemblyHelper
    {
        public static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}