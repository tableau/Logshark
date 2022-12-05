using LogShark.Shared.LogReading.Containers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LogShark.Shared.LogReading.Readers
{
    internal class PrepLogReader : ILogReader
    {
        private readonly String _filePath;
        private readonly IProcessingNotificationsCollector _processingNotificationsCollector;

        private static readonly char PathDelimiter = '\\';

        private MultilineJavaLogReader _javaLogReader;

        private NativeJsonLogsReader _jsonLogReader;

        public static readonly Dictionary<String, PrepLogTypes> PrepLogTypeMap = new Dictionary<String, PrepLogTypes>()
        {
            {@"/floweditor_node.*", PrepLogTypes.MultilineJava},
            {@"^floweditor_node.*", PrepLogTypes.MultilineJava},
            {@"/flowprocessor_node.*", PrepLogTypes.MultilineJava},
            {@"^flowprocessor_node.*", PrepLogTypes.MultilineJava},
            {@"/nativeapi_flowprocessor.*.txt", PrepLogTypes.NativeJson},
            {@"^nativeapi_flowprocessor.*.txt", PrepLogTypes.NativeJson},
            {@"Logs/app.log", PrepLogTypes.NativeJson},
            {@"^app.log", PrepLogTypes.NativeJson},
            {@"Logs/preprestapi.*.log*", PrepLogTypes.NativeJson},
            {@"^preprestapi.*.log*", PrepLogTypes.NativeJson},
            {@"Logs/log_.*.txt*", PrepLogTypes.NativeJson},
            {@"^log_.*.txt*", PrepLogTypes.NativeJson},
        };

        public PrepLogReader(Stream stream, string filePath, IProcessingNotificationsCollector processingNotificationsCollector)
        {
            _filePath = filePath;
            _processingNotificationsCollector = processingNotificationsCollector;

            _jsonLogReader = new NativeJsonLogsReader(stream, filePath, processingNotificationsCollector);
            _javaLogReader = new MultilineJavaLogReader(stream);
        }

        public IEnumerable<ReadLogLineResult> ReadLines()
        {    
            String filename = _filePath.Split(PathDelimiter).Last();  // Get the file name

            foreach(string pattern in PrepLogTypeMap.Keys)
            {
                Regex r = new Regex(pattern);
                if(r.Match(filename).Success)
                {
                    switch(PrepLogTypeMap[pattern])
                    {
                        case PrepLogTypes.MultilineJava:
                            return _javaLogReader.ReadLines();
                        case PrepLogTypes.NativeJson:
                            return _jsonLogReader.ReadLines();
                    }
                }
            }

            _processingNotificationsCollector.ReportError($"Can't map file to a type of log, filename: {0}. " +
                $"Falling back to multiline java log", _filePath);

            return _jsonLogReader.ReadLines();
        }
    }

    internal enum PrepLogTypes
    {
        NativeJson,
        MultilineJava,
    }
}
