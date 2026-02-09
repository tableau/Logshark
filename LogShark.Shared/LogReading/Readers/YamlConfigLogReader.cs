using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogShark.Shared.LogReading.Containers;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace LogShark.Shared.LogReading.Readers
{
    public class YamlConfigLogReader : ILogReader
    {
        private readonly Stream _stream;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;
        public YamlConfigLogReader(Stream stream, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _stream = stream;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            var wholeFileAsLines = new SimpleLinePerLineReader(_stream)
                .ReadLines()
                .Select(readLineResult => readLineResult.LineContent as string ?? string.Empty);
            var wholeFileAsString = string.Join(Environment.NewLine, wholeFileAsLines);
            try
            {
                var configValues = ParseYamlConfigContentsIntoDictionary(wholeFileAsString);
                return new List<ReadLogLineResult> { new ReadLogLineResult(0, configValues) };
            }
            catch (Exception ex)
            {
                //This is likely happening due to a very long line in config file which got truncated
                _processingNotificationsCollector.ReportWarning("Reading Config file failed. Attempting to fix the file. Some lines will be removed", "YamlConfigLogReader");

                try
                {
                    var cleansedFileAsLines = cleanseConfigFile(wholeFileAsString);
                    var configValues = ParseYamlConfigContentsIntoDictionary(cleansedFileAsLines);
                    return new List<ReadLogLineResult> { new ReadLogLineResult(0, configValues) };
                }
                catch(Exception e)
                {
                    _processingNotificationsCollector.ReportError("Reading Config file failed. Could not fix the file", "YamlConfigLogReader");
                    return new List<ReadLogLineResult> { new ReadLogLineResult(0, null) };

                }


            }

           
        }
        
        private static IDictionary<string, string> ParseYamlConfigContentsIntoDictionary(string fileContents)
        {
            var stringReader = new StringReader(fileContents);
            var parser = new Parser(stringReader);
            var deserializer = new Deserializer();

            parser.Consume<StreamStart>();
            parser.Accept<DocumentStart>(out var _);

            var document = deserializer.Deserialize(parser);
            var rawDictionary = document as IDictionary<object, object>;
            return rawDictionary?.ToDictionary(k => k.Key.ToString(), k => SerializeValue(k));
        }
        private  string cleanseConfigFile(string wholeFileAsLines)
        {

            List<string> lines = wholeFileAsLines.Split('\n').ToList();
            List<string> errors = new List<string>();   
            foreach (var line in lines)
            {
                if (line.Contains("truncated"))
                {
                    errors.Add(line);
                    var configName = line.Split(':')[0];
                    _processingNotificationsCollector.ReportWarning("Config {configName} has been removed", "YamlConfigLogReader");
                }

            }
            foreach (var line in errors)
            {
               lines.Remove(line);  
            }   
            var cleansedFileAsString = string.Join(Environment.NewLine, lines);
            return cleansedFileAsString;
        }
        private static string SerializeValue(KeyValuePair<object, object> kvp)
        {
            return !(kvp.Value is string) && kvp.Value is IEnumerable<object> enumerableValue
                ? string.Join(",", enumerableValue)
                : kvp.Value?.ToString();
        }
    }
}