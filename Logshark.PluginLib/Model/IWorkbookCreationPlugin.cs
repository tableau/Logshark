using System.Collections.Generic;
using System.Xml;

namespace Logshark.PluginLib.Model
{
    public interface IWorkbookCreationPlugin
    {
        ICollection<string> WorkbookNames { get; } 
        
        XmlDocument GetWorkbookXml(string workbookName);
    }
}