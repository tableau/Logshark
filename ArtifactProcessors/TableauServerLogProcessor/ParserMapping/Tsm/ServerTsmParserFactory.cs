using LogParsers.Base;
using LogParsers.Base.Helpers;
using LogParsers.Base.ParserBuilders;
using Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm.ParserBuilders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Logshark.ArtifactProcessors.TableauServerLogProcessor.ParserMapping.Tsm
{
    internal class ServerTsmParserFactory : BaseParserFactory, IParserFactory
    {
        private static readonly IDictionary<string, Type> DirectoryMapStatic = new Dictionary<string, Type>
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
            { @"gateway", typeof(HttpdParserBuilder) },
            { @"hyper", typeof(HyperParserBuilder) },
            { @"licenseservice", typeof(LicenseServiceParserBuilder) },
            { @"pgsql", typeof(PgsqlParserBuilder) },
            { @"samlservice", typeof(SamlServiceParserBuilder) },
            { @"searchserver", typeof(SearchServerParserBuilder) },
            { @"siteimportexport", typeof(SiteImportExportParserBuilder) },
            { @"sysinfo", typeof(NetstatLinuxParserBuilder) },
            { @"tabadminagent", typeof(TabAdminAgentParserBuilder) },
            { @"tabadminagent_", typeof(NetstatWindowsParserBuilder)}, // ugly hack to avoid duplicate with actual tabadminagent logs
            { @"tabadmincontroller", typeof(TabAdminControllerParserBuilder) },
            { @"tabsvc", typeof(TabSvcParserBuilder) },
            { @"vizportal", typeof(VizPortalParserBuilder) },
            { @"vizqlserver", typeof(VizqlServerParserBuilder) }
        };

        private readonly IDictionary<Regex, string> regexMap = new Dictionary<Regex, string>();

        public ServerTsmParserFactory(string rootLogLocation) : base(rootLogLocation)
        {
            var manuallyDefinedKeys = new HashSet<string> {"config", "sysinfo", "tabadminagent_"};
            
            const string configRegString = @"[^\\]+\\[^\\]+\\config\\[^\\]+";
            regexMap.Add(new Regex(configRegString, RegexOptions.Compiled), "config");
            
            const string netstatLinuxRegString = @"[^\\]+\\tabadminagent_[^\\]+\\sysinfo";
            regexMap.Add(new Regex(netstatLinuxRegString, RegexOptions.Compiled), "sysinfo");
            
            const string netstatWindowsRegString = @"[^\\]+\\tabadminagent_[^\\]";
            regexMap.Add(new Regex(netstatWindowsRegString, RegexOptions.Compiled), "tabadminagent_");
            
            foreach (var key in DirectoryMapStatic.Keys)
            {
                if (!manuallyDefinedKeys.Contains(key))
                {
                    // One or more characters that is not path separator char
                    var regString = $@"[^\\]+\\{key}_\d+[^\\]*\\logs";
                    regexMap.Add(new Regex(regString, RegexOptions.Compiled), key);
                }
            }
        }

        /// <summary>
        /// Retrieve the correct parser builder for a given file.
        /// </summary>
        /// <param name="fileName">The absolute path to a log file.</param>
        /// <returns>ParserBuilder object for the file.</returns>
        protected override IParserBuilder GetParserBuilder(string fileName)
        {
            // Get a list of all the subdirectories between this log file and the root of the extracted log zip,
            // then recursively walk that list looking for matches to our DirectoryMap dictionary.
            var parentDirs = ParserUtil.GetParentLogDirs(fileName, rootLogLocation);

            var relativeDirectoryPath = string.Join(Path.DirectorySeparatorChar.ToString(), parentDirs);
            foreach (var reg in regexMap.Keys)
            {
                if (reg.IsMatch(relativeDirectoryPath) && DirectoryMap.ContainsKey(regexMap[reg]))
                {
                    var parserBuilderType = DirectoryMap[regexMap[reg]];
                    return Activator.CreateInstance(parserBuilderType) as IParserBuilder; 
                }
            }

            // If we didn't find a match for the directory this log lives in, try the root parser builder.
            return GetRootParserBuilder();
        }

        protected override IDictionary<string, Type> DirectoryMap => DirectoryMapStatic;

        protected override IParserBuilder GetRootParserBuilder()
        {
            return new RootParserBuilder();
        }
    }
}