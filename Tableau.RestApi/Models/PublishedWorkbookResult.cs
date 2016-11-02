using System;

namespace Tableau.RestApi.Models
{
    /// <summary>
    /// Encapsulates state about a published workbook.
    /// </summary>
    public class PublishedWorkbookResult
    {
        public PublishWorkbookRequest Request { get; private set; }
        public DateTime PublishDate { get; private set; }

        public string WorkbookId { get; set; }
        public Uri Uri { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

        public PublishedWorkbookResult(PublishWorkbookRequest request)
        {
            Request = request;
            PublishDate = DateTime.UtcNow;
        }
    }
}
