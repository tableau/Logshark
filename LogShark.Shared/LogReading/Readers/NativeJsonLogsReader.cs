using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;

namespace LogShark.Shared.LogReading.Readers
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
            _processingNotificationsCollector = processingNotificationsCollector; 
            _stream = stream;

            _serializerSettings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
            };
        }
        
        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            return new SimpleLinePerLineReader(_stream)
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
            var lineMaxLength = 100000;



            try
            {


                if (lineString.Length >= lineMaxLength)
                {
                    //query too long and complicated to fix, so just remove the whole query and leave a warning message
                    if (lineString.Contains("query-compiled"))
                    {
                        string lookfor = "query-compiled\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Query is too long to parse\"}]}]}}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long query removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }
                   
                    else if (lineString.Contains("\"query\":"))
                    {
                        string lookfor = "query\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Query is too long to parse\"}}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long query removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);

                    }
                    else if (lineString.Contains("sample-compute"))
                    {

                        string lookfor = "\"sample-domain-sizes\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Sample is too long to parse\"}}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long sample removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }
                    else if (lineString.Contains("\"v\":"))
                    {
                        string lookfor = "\"v\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Logical Query is too long to parse\"}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long logical query removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }
                    /*else if (lineString.Contains("query-plan"))
                    {

                        string lookfor = "\"v\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Query plan is too long to parse\"}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long query plan removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }
                    else if (lineString.Contains("distribute-plan"))
                    {
                        string lookfor = "\"v\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Query plan is too long to parse\"}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long query plan removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }
                    else if (lineString.Contains("one-time-sql"))
                    {
                        string lookfor = "\"v\":";
                        int startIndex = lineString.IndexOf(lookfor) + lookfor.Length;
                        var repairedLineString = lineString.Remove(startIndex).Insert(startIndex, "\"Query plan is too long to parse\"}");
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                        _processingNotificationsCollector.ReportWarning("Long query plan removed - ", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }*/
                    else
                    {
                        var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(lineString, _serializerSettings);
                        return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                    }
                }
                else
                {
                    var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(lineString, _serializerSettings);
                    return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                }
            }
            catch (JsonReaderException ex) when (ex.Message.StartsWith(@"Invalid JavaScript property identifier character:"))
            {
                try
                {
                    // Check for and repair properties using an incorrect ',' character as a number decimal separator in unquoted properties
                    var repairedLineString = NumberDecimalSeparatorRepairRegex.Replace(lineString, "$1.$2");
                    var deserializedObject = JsonConvert.DeserializeObject<NativeJsonLogsBaseEvent>(repairedLineString, _serializerSettings);
                    _processingNotificationsCollector.ReportWarning("Invalid Json found and repaired", _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                    return new ReadLogLineResult(originalEvent.LineNumber, deserializedObject);
                }
                catch (JsonException e) when (lineString.Contains("truncated"))
                {
                    _processingNotificationsCollector.ReportMissedLines(lineString);
                    _processingNotificationsCollector.ReportError(e.Message, _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                    return new ReadLogLineResult(originalEvent.LineNumber, null);
                }
            }
            catch(JsonReaderException ex)
            {
                _processingNotificationsCollector.ReportMissedLines(lineString);
                _processingNotificationsCollector.ReportError(ex.Message, _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                return new ReadLogLineResult(originalEvent.LineNumber, null);
            }
            catch (System.Exception e)
            {
                //this is bad, it can happen when new line character is not clearly detected
                _processingNotificationsCollector.ReportError(e.Message, _filePath, originalEvent.LineNumber, nameof(NativeJsonLogsReader));
                _processingNotificationsCollector.ReportMissedLines(lineString);
                return new ReadLogLineResult(originalEvent.LineNumber, null);
            }
        
               
            
            
        }
    }
}