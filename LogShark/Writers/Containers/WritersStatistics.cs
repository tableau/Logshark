using System.Collections.Generic;
using LogShark.Containers;

namespace LogShark.Writers.Containers
{
    public class WritersStatistics
    {
        public Dictionary<DataSetInfo, WriterLineCounts> DataSets { get; }

        public WritersStatistics(Dictionary<DataSetInfo, WriterLineCounts> dataSets)
        {
            DataSets = dataSets;
        }
    }
}