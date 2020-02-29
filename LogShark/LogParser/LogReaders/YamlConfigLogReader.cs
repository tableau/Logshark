using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogShark.LogParser.Containers;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace LogShark.LogParser.LogReaders
{
    public class YamlConfigLogReader : ILogReader
    {
        private readonly Stream _stream;
        private readonly string _filePath;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        public YamlConfigLogReader(Stream stream, string filePath, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _stream = stream;
            _filePath = filePath;
            _processingNotificationsCollector = processingNotificationsCollector;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            var wholeFileAsLines = new SimpleLinePerLineReader(_stream, _filePath, _processingNotificationsCollector)
                .ReadLines()
                .Select(readLineResult => readLineResult.LineContent as string ?? string.Empty);
            var wholeFileAsString = string.Join(Environment.NewLine, wholeFileAsLines);
            var configValues = ParseYamlConfigContentsIntoDictionary(wholeFileAsString);

            return new List<ReadLogLineResult> {new ReadLogLineResult(0, configValues)};
        }
        
        private static IDictionary<string, string> ParseYamlConfigContentsIntoDictionary(string fileContents)
        {
            var stringReader = new StringReader(fileContents);
            var parser = new Parser(stringReader);
            var deserializer = new Deserializer();

            parser.Expect<StreamStart>();
            parser.Accept<DocumentStart>();
                
            var document = deserializer.Deserialize(parser);
            var rawDictionary = document as IDictionary<object, object>;
            return rawDictionary?.ToDictionary(k => k.Key.ToString(), k => SerializeValue(k));
        }

        public static string SerializeValue(KeyValuePair<object, object> kvp)
        {
            return !(kvp.Value is string) && kvp.Value is IEnumerable<object> enumerableValue
                ? String.Join(',', enumerableValue)
                : kvp.Value?.ToString();           
        }
    }
}