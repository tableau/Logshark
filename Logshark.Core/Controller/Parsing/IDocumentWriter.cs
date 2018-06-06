using Newtonsoft.Json.Linq;

namespace Logshark.Core.Controller.Parsing
{
    internal interface IDocumentWriter
    {
        DocumentWriteResult Write(JObject document);

        void Shutdown();
    }
}