using System.Collections.Generic;
using LogShark.Containers;

namespace LogShark.Writers.Containers
{
    public class WorkbookGeneratorResults
    {
        public IList<CompletedWorkbookInfo> CompletedWorkbooks { get; }
        public IList<PackagedWorkbookTemplateInfo> WorkbookTemplates { get; }

        public WorkbookGeneratorResults(IList<CompletedWorkbookInfo> completedWorkbooks, IList<PackagedWorkbookTemplateInfo> workbookTemplates)
        {
            CompletedWorkbooks = completedWorkbooks;
            WorkbookTemplates = workbookTemplates;
        }
    }
}