using System.Collections.Generic;
using System.IO;

namespace Logshark.PluginLib.Model
{
    public interface IWorkbookCreationPlugin
    {
        ICollection<string> WorkbookNames { get; }

        Stream GetWorkbook(string workbookName);
    }
}