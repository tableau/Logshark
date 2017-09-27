using ICSharpCode.SharpZipLib.Zip;
using Logshark.Core.Helpers;
using Logshark.RequestModel;
using System.IO;

namespace Logshark.Core.Controller.Extraction
{
    /// <summary>
    /// Unzipper derived class that is specifically for unzipping Tableau logsets.
    /// </summary>
    internal class LogsetUnzipper : Unzipper
    {
        protected LogsharkRequest request;

        public LogsetUnzipper(UnzipStrategy unzipStrategy, LogsharkRequest request)
            : base(unzipStrategy)
        {
            this.request = request;
        }

        protected override bool QualifiesForExtraction(ZipEntry zipEntry, string destinationDirectory)
        {
            // Disqualify any zip entry that contains an illegal path character, as valid Tableau log files do not.
            if (ContainsInvalidPathCharacters(zipEntry.Name))
            {
                return false;
            }

            return base.QualifiesForExtraction(zipEntry, destinationDirectory);
        }
    }
}