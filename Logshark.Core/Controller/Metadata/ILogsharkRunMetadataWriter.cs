namespace Logshark.Core.Controller.Metadata
{
    internal interface ILogsharkRunMetadataWriter
    {
        void WriteMetadata(LogsharkRunContext run);
    }
}