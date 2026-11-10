using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogShark.Shared.HostExtensions
{
    public interface IArtifactPublisher
    {
        bool IsEnabled { get; }

        // Returns null when nothing was published
        Task<ArtifactPublishResults> PublishLargeWorkbooks(string projectName, IList<ArtifactWorkbook> workbooks);
    }

    public class ArtifactWorkbook
    {
        public string Name { get; }
        public string Path { get; }
        public bool HasAnyData { get; }

        public ArtifactWorkbook(string name, string path, bool hasAnyData)
        {
            Name = name;
            Path = path;
            HasAnyData = hasAnyData;
        }
    }

    public class ArtifactWorkbookPublishResult
    {
        public string WorkbookName { get; }
        public bool IsSuccessful { get; }
        public string DownloadUrl { get; }
        public string ErrorMessage { get; }

        public ArtifactWorkbookPublishResult(string workbookName, bool isSuccessful, string downloadUrl, string errorMessage)
        {
            WorkbookName = workbookName;
            IsSuccessful = isSuccessful;
            DownloadUrl = downloadUrl;
            ErrorMessage = errorMessage;
        }
    }

    public class ArtifactPublishResults
    {
        public IReadOnlyList<ArtifactWorkbookPublishResult> UploadResults { get; }

        public ArtifactPublishResults(IReadOnlyList<ArtifactWorkbookPublishResult> uploadResults)
        {
            UploadResults = uploadResults;
        }
    }

    public class NullArtifactPublisher : IArtifactPublisher
    {
        public bool IsEnabled => false;

        public Task<ArtifactPublishResults> PublishLargeWorkbooks(string projectName, IList<ArtifactWorkbook> workbooks)
        {
            return Task.FromResult<ArtifactPublishResults>(null);
        }
    }
}
