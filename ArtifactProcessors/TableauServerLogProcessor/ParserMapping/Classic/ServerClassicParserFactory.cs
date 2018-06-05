using LogParsers.Base;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Classic.ParserBuilders;
using System;
using System.Collections.Generic;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Classic
{
    internal class ServerClassicParserFactory : BaseParserFactory, IParserFactory
    {
        private static readonly IDictionary<string, Type> directoryMap = new Dictionary<string, Type>
        {
            { @"backgrounder", typeof(BackgrounderParserBuilder) },
            { @"cacheserver", typeof(CacheServerParserBuilder) },
            { @"clustercontroller", typeof(ClusterControllerParserBuilder) },
            { @"config", typeof(ConfigParserBuilder) },
            { @"dataengine", typeof(DataEngineParserBuilder) },
            { @"dataserver", typeof(DataServerParserBuilder) },
            { @"filestore", typeof(FileStoreParserBuilder) },
            { @"httpd", typeof(HttpdParserBuilder) },
            { @"hyper", typeof(HyperParserBuilder) },
            { @"licensing", typeof(LicensingParserBuilder) },
            { @"logs", typeof(LogsParserBuilder) },
            { @"pgsql", typeof(PgsqlParserBuilder) },
            { @"searchserver", typeof(SearchServerParserBuilder) },
            { @"service", typeof(ServiceParserBuilder) },
            { @"solr", typeof(SolrParserBuilder) },
            { @"tabadmin", typeof(TabAdminParserBuilder) },
            { @"tabadminservice", typeof(TabAdminServiceParserBuilder) },
            { @"vizportal", typeof(VizPortalParserBuilder) },
            { @"vizqlserver", typeof(VizqlParserBuilder) },
            { @"wgserver", typeof(WgServerParserBuilder) },
            { @"zookeeper", typeof(ZookeeperParserBuilder) }
        };

        public ServerClassicParserFactory(string rootLogLocation) : base(rootLogLocation)
        {
        }

        protected override IDictionary<string, Type> DirectoryMap { get { return directoryMap; } }

        protected override IParserBuilder GetRootParserBuilder()
        {
            return new RootParserBuilder();
        }
    }
}