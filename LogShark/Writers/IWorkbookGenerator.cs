using System.Collections.Generic;
using LogShark.Containers;
using LogShark.Writers.Containers;

namespace LogShark.Writers
{
    public interface IWorkbookGenerator
    {
        WorkbookGeneratorResults CompleteWorkbooksWithResults(WritersStatistics writersStatistics);
        bool GeneratesWorkbooks { get; }
    }
}