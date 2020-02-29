using LogShark.Containers;

namespace LogShark.Writers.Containers
{
    public class WriterLineCounts
    {
        public DataSetInfo DataSetInfo { get; }
        public long LinesPersisted { get; }
        public long NullLinesIgnored { get; }

        public WriterLineCounts(DataSetInfo dataSetInfo, long linesPersisted, long nullLinesIgnored)
        {
            DataSetInfo = dataSetInfo;
            LinesPersisted = linesPersisted;
            NullLinesIgnored = nullLinesIgnored;
        }
    }
}