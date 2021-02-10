using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LogShark.Shared.LogReading.Containers;

namespace LogShark.Shared.LogReading.Readers
{
    public class NetstatWindowsReader : ILogReader
    {
        private static readonly Regex HeaderLineRegex = new Regex(@"^ .+? +.+? +.+? +.+? + .+? + .+?$", RegexOptions.Compiled);

        // Lines like " [svchost.exe]" or " Can not obtain ownership information"
        private static readonly Regex ExecutableInfoRegex = new Regex(@"^ [^ ]", RegexOptions.Compiled);
        
        private bool _passedHeader; 

        private readonly string _filePath;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly Stream _stream;

        public NetstatWindowsReader(Stream stream, string filePath, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _filePath = filePath;
            _stream = stream;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            using (var reader = new StreamReader(_stream))
            {
                string line = null;
                var lineCount = 0;

                while (!_passedHeader && !reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    lineCount++;

                    if (HeaderLineRegex.IsMatch(line))
                    {
                        _passedHeader = true;
                    }
                }

                if (!_passedHeader)
                {
                    _processingNotificationsCollector.ReportError("Failed to find header before reaching EOF. Netstat info will be empty", _filePath, 0, nameof(NetstatWindowsReader));
                }

                var section = new Stack<(string line, int lineNumber)>();
                var sectionStartLine = lineCount + 1;

                while (line != null && !reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    lineCount++;

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        section.Push((line, lineCount));

                        if (ExecutableInfoRegex.IsMatch(line))
                        {
                            yield return new ReadLogLineResult(sectionStartLine, section);

                            section = new Stack<(string line, int lineNumber)>();
                            sectionStartLine = lineCount + 1;
                        }
                    }
                }
            }
        }
    }
}
