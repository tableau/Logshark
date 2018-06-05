using System.Collections.Generic;

namespace Logshark.Core.Controller.Initialization.Archive.Extraction
{
    internal class ExtractionResult
    {
        public string RootLogDirectory { get; protected set; }

        public ICollection<UnzipResult> UnzipResults { get; protected set; }

        public ExtractionResult(string rootLogDirectory, ICollection<UnzipResult> unzipResults)
        {
            RootLogDirectory = rootLogDirectory;
            UnzipResults = unzipResults;
        }
    }
}