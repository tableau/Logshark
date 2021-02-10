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

        public YamlConfigLogReader(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {
            var wholeFileAsLines = new SimpleLinePerLineReader(_stream)
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

            parser.Consume<StreamStart>();
            parser.Accept<DocumentStart>(out var _);
            
            var document = deserializer.Deserialize(parser);
            var rawDictionary = document as IDictionary<object, object>;
            return rawDictionary?.ToDictionary(k => k.Key.ToString(), k => SerializeValue(k));
        }

        private static string SerializeValue(KeyValuePair<object, object> kvp)
        {
            return !(kvp.Value is string) && kvp.Value is IEnumerable<object> enumerableValue
                ? string.Join(",", enumerableValue)
                : kvp.Value?.ToString();
        }
    }
}