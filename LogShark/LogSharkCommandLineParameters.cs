using System.IO;

namespace LogShark
{
    public class LogSharkCommandLineParameters
    {
        private string _originalFileName;
        private string _originalLocation;

        public string AppendTo { get; set; }
        public string DatabaseConnectionString { get; set; }
        public string DatabaseHost { get; set; }
        public string DatabaseName { get; set; }
        public string DatabasePassword { get; set; }
        public string DatabaseUsername { get; set; }
        public bool EmbedCredentialsOnPublish { get; set; }
        public bool ForceRunId { get; set; }
        public string LogSetLocation { get; set; }

        public string OriginalFileName
        {
            get => _originalFileName ?? Path.GetFileName(OriginalLocation);
            set => _originalFileName = value;
        }

        public string OriginalLocation
        {
            get => _originalLocation ?? LogSetLocation;
            set => _originalLocation = value;
        }

        public bool PublishWorkbooks { get; set; }
        public string RequestedPlugins { get; set; }
        public string RequestedWriter { get; set; }
        public string UserProvidedRunId { get; set; }
        public string TableauServerUsername { get; set; }
        public string TableauServerPassword { get; set; }
        public string TableauServerProjectDescriptionFooterHtml { get; set; }
        public string TableauServerSite { get; set; }
        public string TableauServerUrl { get; set; }
        public string WorkbookNameSuffixOverride { get; set; }

        public LogSharkCommandLineParameters GetShallowCopy()
        {
            return (LogSharkCommandLineParameters) MemberwiseClone();
        }
    }
}