using System.Collections.Generic;
using LogShark.Writers.Containers;

namespace LogShark.Containers
{
    public class SinglePluginExecutionResults
    {
        public IList<string> AdditionalTags { get; }
        public bool HasAdditionalTags => AdditionalTags != null && AdditionalTags.Count > 0;
        public IList<WriterLineCounts> WritersStatistics { get; }

        public SinglePluginExecutionResults(WriterLineCounts writerLineCounts) : this(new List<WriterLineCounts> { writerLineCounts }, new List<string>())
        {
        }
        
        public SinglePluginExecutionResults(IEnumerable<WriterLineCounts> writersLineCounts) : this(new List<WriterLineCounts>(writersLineCounts), new List<string>())
        {
        }

        public SinglePluginExecutionResults(IList<WriterLineCounts> writersStatistics, IList<string> additionalTags)
        {
            WritersStatistics = writersStatistics;
            AdditionalTags = additionalTags;
        }
    }
}