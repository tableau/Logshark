using LogParsers.Base;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm
{
    internal class ServerTsmParserFactory : BaseParserFactory, IParserFactory
    {
        private static readonly IDictionary<string, Type> directoryMap = new Dictionary<string, Type>
        {
            { @"appzookeeper", typeof(AppZookeeperParserBuilder) },
            { @"backgrounder", typeof(BackgrounderParserBuilder) },
            { @"backuprestore", typeof(BackupRestoreParserBuilder) },
            { @"cacheserver", typeof(CacheServerParserBuilder) },
            { @"clustercontroller", typeof(ClusterControllerParserBuilder) },
            { @"config", typeof(ConfigParserBuilder) },
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
            { @"tabadminagent", typeof(TabAdminAgentParserBuilder) },
            { @"tabadmincontroller", typeof(TabAdminControllerParserBuilder) },
            { @"tabsvc", typeof(TabSvcParserBuilder) },
            { @"vizportal", typeof(VizPortalParserBuilder) },
            { @"vizqlserver", typeof(VizqlServerParserBuilder) }
        };

        public ServerTsmParserFactory(string rootLogLocation) : base(rootLogLocation)
        {
        }

        protected override IDictionary<string, Type> DirectoryMap { get { return directoryMap; } }

        protected override IParserBuilder GetRootParserBuilder()
        {
            return new RootParserBuilder();
        }
    }
}