using System.Collections.Generic;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Writers.Containers;
using static Tools.TableauServerRestApi.Containers.PublishWorkbookRequest;

namespace LogShark.Writers
{
    public interface IWorkbookPublisher
    {
        Task<PublisherResults> PublishWorkbooks(
            string projectName,
            IEnumerable<CompletedWorkbookInfo> completedWorkbooks,
            IEnumerable<string> workbookTags);
    }
}