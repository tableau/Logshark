using ServiceStack.DataAnnotations;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Metadata.Run
{
    public class LogsharkCustomMetadata
    {
        private readonly KeyValuePair<string, object> requestMetadataItem;
        private readonly LogsharkRunMetadata runMetadata;

        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [Index]
        [References(typeof(LogsharkRunMetadata))]
        public int LogsharkRunMetadataId
        {
            get { return runMetadata.Id; }
        }

        [Index]
        public string Key
        {
            get { return requestMetadataItem.Key; }
        }

        public string Value
        {
            get { return requestMetadataItem.Value.ToString(); }
        }

        public LogsharkCustomMetadata()
        {
        }

        public LogsharkCustomMetadata(KeyValuePair<string, object> requestMetadataItem, LogsharkRunMetadata runMetadata)
        {
            this.requestMetadataItem = requestMetadataItem;
            this.runMetadata = runMetadata;
        }
    }
}