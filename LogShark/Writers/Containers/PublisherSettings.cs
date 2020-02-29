using System.Collections.Generic;
using Tools.TableauServerRestApi.Containers;

namespace LogShark.Writers.Containers
{
    public class PublisherSettings
    {
        public bool ApplyPluginProvidedTagsToWorkbooks { get; }
        public TableauServerInfo TableauServerInfo { get; }
        public List<string> GroupsToProvideWithDefaultPermissions { get; }
        public (string Id, string Name) ParentProjectInfo { get; }

        public PublisherSettings(
            TableauServerInfo tableauServerInfo,
            List<string> groupsToProvideWithDefaultPermissions,
            bool applyPluginProvidedTagsToWorkbooks,
            string parentProjectId,
            string parentProjectName)
        {
            ApplyPluginProvidedTagsToWorkbooks = applyPluginProvidedTagsToWorkbooks;
            TableauServerInfo = tableauServerInfo;
            GroupsToProvideWithDefaultPermissions = groupsToProvideWithDefaultPermissions;
            ParentProjectInfo = (parentProjectId, parentProjectName);
        }
    }
}