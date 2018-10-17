using Logshark.PluginLib.Extensions;
using Logshark.Plugins.ClusterController.Helpers;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.ClusterController.Models
{
    public sealed class ClusterControllerDiskIoSample : BaseClusterControllerEvent
    {
        public string Device { get; set; }

        public double? ReadsPerSec { get; set; }

        public double? ReadBytesPerSec { get; set; }

        public double? WritesPerSec { get; set; }

        public double? WriteBytesPerSec { get; set; }

        public double? QueueLength { get; set; }

        public ClusterControllerDiskIoSample()
        {
        }

        public ClusterControllerDiskIoSample(BsonDocument document) : base(document)
        {
            string message = document.GetString("message");
            IDictionary<string, string> fieldsInMessage = GetFieldsFromMessage(message);

            Device = GetFieldAsString(fieldsInMessage, "device");
            ReadsPerSec = GetFieldAsDouble(fieldsInMessage, "reads");
            ReadBytesPerSec = GetFieldAsDouble(fieldsInMessage, "readBytes");
            WritesPerSec = GetFieldAsDouble(fieldsInMessage, "writes");
            WriteBytesPerSec = GetFieldAsDouble(fieldsInMessage, "writeBytes");
            QueueLength = GetFieldAsDouble(fieldsInMessage, "queue");
        }

        private IDictionary<string, string> GetFieldsFromMessage(string message)
        {
            IDictionary<string, string> fieldsInMessage = new Dictionary<string, string>();

            IEnumerable<string> keyValuePairs = message.Replace(ClusterControllerConstants.DISK_IO_MONITOR_MESSAGE_PREFIX, "")
                                                       .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(item => item.Trim());

            foreach (string keyValuePair in keyValuePairs)
            {
                int indexOfDelimiter = keyValuePair.IndexOf(":", StringComparison.OrdinalIgnoreCase);

                if (indexOfDelimiter >= 1)
                {
                    string fieldKey = keyValuePair.Substring(0, indexOfDelimiter);
                    string fieldValue = keyValuePair.Substring(indexOfDelimiter + 1, keyValuePair.Length - indexOfDelimiter - 1);

                    if (!String.IsNullOrWhiteSpace(fieldKey))
                    {
                        fieldsInMessage.Add(fieldKey, fieldValue);
                    }
                }
            }

            return fieldsInMessage;
        }

        private string GetFieldAsString(IDictionary<string, string> fieldDictionary, string fieldName)
        {
            if (!fieldDictionary.ContainsKey(fieldName))
            {
                return fieldDictionary[fieldName];
            }

            return null;
        }

        private double? GetFieldAsDouble(IDictionary<string, string> fieldDictionary, string fieldName)
        {
            if (fieldDictionary.ContainsKey(fieldName))
            {
                double fieldAsDouble;
                if (Double.TryParse(fieldDictionary[fieldName], out fieldAsDouble))
                {
                    return fieldAsDouble;
                }
            }

            return null;
        }
    }
}