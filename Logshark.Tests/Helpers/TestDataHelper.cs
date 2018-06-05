using NUnit.Framework;
using System.IO;

namespace Logshark.Tests.Helpers
{
    internal static class TestDataHelper
    {
        private const string TestDataDirectoryName = "_TestData";

        public static string GetDataDirectory(string testNameSpace = "")
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, testNameSpace, TestDataDirectoryName);
        }

        public static string GetResourcePath(string name, string testNameSpace = "")
        {
            return Path.Combine(GetDataDirectory(testNameSpace), name);
        }

        public static string GetServerLogProcessorResourcePath(string name)
        {
            return GetResourcePath(name, "ServerLogProcessorTests");
        }
    }
}