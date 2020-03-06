using System.Collections.Generic;
using System.Threading.Tasks;
using LogShark.Containers;
using LogShark.Writers.Containers;
using Microsoft.Extensions.Logging;
using Tools.TableauServerRestApi;
using static Tools.TableauServerRestApi.Containers.PublishWorkbookRequest;

namespace LogShark.Writers.Csv
{
    public class CsvBasedWorkbookPublisher : IWorkbookPublisher
    {
        private readonly ILogger _logger;

        public CsvBasedWorkbookPublisher(PublisherSettings publisherSettings, ILogger logger)
        {
            _logger = logger;
        }

        public Task<PublisherResults> PublishWorkbooks(string projectName, string projectDescription, IEnumerable<CompletedWorkbookInfo> completedWorkbooks, IEnumerable<string> workbookTags)
        {
            throw new System.NotImplementedException();
        }
    }
}