using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LogShark.LogParser.Containers;

namespace LogShark.LogParser.LogReaders
{
    public class MultilineJavaLogReader : ILogReader
    {
        private static readonly Regex JavaLogsTimestampRegex  = new Regex(@"^\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{3}\s", RegexOptions.Compiled);

        private readonly Stream _stream;

        public MultilineJavaLogReader(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            using (var reader = new StreamReader(_stream))
            {
                var currentProcessingData = new CurrentProcessingData
                {
                    LineNumber = 0,
                    BufferedLine = null
                };
            
                string line;
                while ((line = ReadNextLine(reader, currentProcessingData)) != null)
                {
                    var numberOfTheFirstLine = currentProcessingData.LineNumber;

                    var sb = new StringBuilder();
                    sb.Append(line);

                    var nextLineHasNoMarker = true;
                    while (nextLineHasNoMarker)
                    {
                        var nextLine = ReadNextLine(reader, currentProcessingData);

                        if (nextLine == null) // We've reached end of file
                        {
                            break;
                        }

                        nextLineHasNoMarker = !JavaLogsTimestampRegex.IsMatch(nextLine);

                        if (nextLineHasNoMarker)
                        {
                            sb.Append("\n" + nextLine);
                        }
                        else
                        {
                            currentProcessingData.BufferedLine = nextLine;
                        }
                    }

                    yield return new ReadLogLineResult(numberOfTheFirstLine, sb.ToString());
                }
            }
        }

        private static string ReadNextLine(TextReader reader, CurrentProcessingData currentProcessingData)
        {
            if (currentProcessingData.BufferedLine != null)
            {
                var line = currentProcessingData.BufferedLine;
                currentProcessingData.BufferedLine = null;
                return line;
            } 

            ++currentProcessingData.LineNumber;
            var nextLine = reader.ReadLine();
            return nextLine;
        }

        private class CurrentProcessingData
        {
            public int LineNumber { get; set; }
            public string BufferedLine { get; set; }
        }
    }
}