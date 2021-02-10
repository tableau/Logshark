using LogShark.Writers.Containers;
using Tools.TableauServerRestApi.Containers;

namespace LogShark.Extensions
{
    public static class LogSharkConfigurationExtensions
    {
        public static PublisherSettings GetPublisherSettings(this LogSharkConfiguration logSharkConfiguration)
        {
            var tableauServerInfo = new TableauServerInfo(
                logSharkConfiguration.TableauServerUrl,
                logSharkConfiguration.TableauServerSite,
                logSharkConfiguration.TableauServerUsername,
                logSharkConfiguration.TableauServerPassword,
                logSharkConfiguration.TableauServerTimeout,
                logSharkConfiguration.TableauServerPublishingTimeout);
            return new PublisherSettings(
                tableauServerInfo,
                logSharkConfiguration.GroupsToProvideWithDefaultPermissions,
                logSharkConfiguration.ApplyPluginProvidedTagsToWorkbooks,
                logSharkConfiguration.ParentProjectId,
                logSharkConfiguration.ParentProjectName);
        }
    }
}