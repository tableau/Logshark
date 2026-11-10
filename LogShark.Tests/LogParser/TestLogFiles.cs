using System.IO;

namespace LogShark.Tests.LogParser
{
    public static class TestLogFiles
    {
        private const string TestFileEmpty = "TestData/ILogReaderTestLogs/TestLogFileEmpty.log";
        private const string TestFileWithCsv = "TestData/ILogReaderTestLogs/TestLogFileWithCsvData.log";
        private const string TestFileWithJson = "TestData/ILogReaderTestLogs/TestLogFileWithJson.log";
        private const string TestFileWithPlainLines = "TestData/ILogReaderTestLogs/TestLogFileWithPlainLines.log";
        private const string TestFileWithYaml = "TestData/ILogReaderTestLogs/TestLogFileWithYamlData.log";
        private const string TestFileWithWindowsNetstat = "TestData/ILogReaderTestLogs/netstat-windows.txt";
        private const string TestFileWithLocalizedWindowsNetstat = "TestData/ILogReaderTestLogs/netstat-windows-shift_jis.txt";

        public static Stream OpenEmptyTestFile()
        {
            return File.Open(TestFileEmpty, FileMode.Open);
        }

        public static Stream OpenTestFileWithPlainLines()
        {
            return File.Open(TestFileWithPlainLines, FileMode.Open);
        }

        public static Stream OpenTestFileWithYamlData()
        {
            return File.Open(TestFileWithYaml, FileMode.Open);
        }

        public static Stream OpenTestFileWithJsonData()
        {
            return File.Open(TestFileWithJson, FileMode.Open);
        }

        public static Stream OpenTestFileWithCsvData()
        {
            return File.Open(TestFileWithCsv, FileMode.Open);
        }

        public static Stream OpenTestFileWithWindowsNetstatData()
        {
            return File.Open(TestFileWithWindowsNetstat, FileMode.Open);
        }

        public static Stream OpenTestFileWithLocalizedWindowsNetstatData()
        {
            return File.Open(TestFileWithLocalizedWindowsNetstat, FileMode.Open);
        }
    }
}