using ICSharpCode.SharpZipLib.Zip;

namespace Logshark.Core.Controller.Initialization.Archive.Extraction
{
    /// <summary>
    /// Unzipper derived class that is specifically for unzipping Tableau logsets.
    /// </summary>
    internal class LogsetUnzipper : Unzipper
    {
        public LogsetUnzipper(UnzipStrategy unzipStrategy)
            : base(unzipStrategy)
        {
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