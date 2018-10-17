using LogParsers.Base;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm
{
    internal class ServerTsmLegacyParserFactory : BaseParserFactory, IParserFactory
    {
        private static readonly IDictionary<string, Type> DirectoryMapStatic = new Dictionary<string, Type>
        {
            { @"appzookeeper", typeof(AppZookeeperParserBuilder) },
            { @"backgrounder", typeof(BackgrounderParserBuilder) },
            { @"backuprestore", typeof(BackupRestoreParserBuilder) },
            { @"cacheserver", typeof(CacheServerParserBuilder) },
            { @"clustercontroller", typeof(ClusterControllerParserBuilder) },
            //{ @"config", typeof(ConfigParserBuilder) }, 2018.1 Linux does not seem to include config information into logset
            { @"databasemaintenance", typeof(DatabaseMaintenanceParserBuilder) },
            { @"dataserver", typeof(DataServerParserBuilder) },
            { @"filestore", typeof(FileStoreParserBuilder) },
            { @"httpd", typeof(HttpdParserBuilder) },
            { @"hyper", typeof(HyperParserBuilder) },
            { @"licenseservice", typeof(LicenseServiceParserBuilder) },
            { @"pgsql", typeof(PgsqlParserBuilder) },
            { @"samlservice", typeof(SamlServiceParserBuilder) },
            { @"searchserver", typeof(SearchServerParserBuilder) },
            { @"siteimportexport", typeof(SiteImportExportParserBuilder) },
            { @"sysinfo", typeof(NetstatLinuxParserBuilder) },
            { @"tabadminagent", typeof(TabAdminAgentParserBuilder) },
            { @"tabadmincontroller", typeof(TabAdminControllerParserBuilder) },
            { @"tabsvc", typeof(TabSvcParserBuilder) },
            { @"vizportal", typeof(VizPortalParserBuilder) },
            { @"vizqlserver", typeof(VizqlServerParserBuilder) }
        };

        public ServerTsmLegacyParserFactory(string rootLogLocation) : base(rootLogLocation)
        {
        }

        protected override IDictionary<string, Type> DirectoryMap => DirectoryMapStatic;

        protected override IParserBuilder GetRootParserBuilder()
        {
            return new RootParserBuilder();
        }
    }
}