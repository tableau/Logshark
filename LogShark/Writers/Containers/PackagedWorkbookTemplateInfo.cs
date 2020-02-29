using System.Collections.Generic;
using LogShark.Containers;

namespace LogShark.Writers.Containers
{
    public class PackagedWorkbookTemplateInfo
    {
        public string Name { get; }
        public string Path { get; }
        public ISet<string> RequiredExtracts { get; }

        public PackagedWorkbookTemplateInfo(string name, string path, ISet<string> requiredExtracts)
        {
            Name = name;
            Path = path;
            RequiredExtracts = requiredExtracts;
        }
    }
}