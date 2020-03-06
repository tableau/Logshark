using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.LogParser.Containers;
using LogShark.Plugins.Shared;
using Newtonsoft.Json;

namespace LogShark.LogParser.LogReaders
{
    public class NativeJsonLogsReader : ILogReader
    {
        private readonly string _filePath;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly Stream _stream;
        
        private static readonly Regex NumberDecimalSeparatorRepairRegex = new Regex(@"(""[^""]+""\s*:\s*\d+),(\d+)", RegexOptions.Compiled);
            
        public NativeJsonLogsReader(Stream stream, string filePath, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _filePath = filePath;
            _processingNotificationsCollector = processingNotificationsCollector; ;
            _stream = stream;

            _serializerSettings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
            };
        }
        
        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            return new SimpleLinePerLineReader(_stream, _filePath, _processingNotificationsCollector)
                .ReadLines()
                .Select(TurnStringIntoNativeJsonLogBaseEvent);
        }

        private ReadLogLineResult TurnStringIntoNativeJsonLogBaseEvent(ReadLogLineResult originalEvent)
        {
            if (!(originalEvent.LineContent is string lineString))
            {
                _processingNotificationsCollector.ReportError("Can't interpret line as string", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                return new ReadLogLineResult(originalEvent.LineNumber, null);
            }

            try
            {
                try
                {
                    var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(lineString, _serializerSettings);
                    return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                }
                catch (JsonReaderException ex) when (ex.Message.StartsWith(@"Invalid JavaScript property identifier character:"))
                {
                    // Check for and repair properties using an incorrect ',' character as a number decimal separator in unquoted properties
                    var repairedLineString = NumberDecimalSeparatorRepairRegex.Replace(lineString, "$1.$2");
                    var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                    _processingNotificationsCollector.ReportWarning("Invalid Json found and repaired", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                    return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                }
            }
            catch (JsonException ex)
            {
                _processingNotificationsCollector.ReportError(ex.Message, _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                return new ReadLogLineResult(originalEvent.LineNumber, null);
            }
        }
    }
}