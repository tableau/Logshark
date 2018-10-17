using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Netstat.Helpers
{
    internal static class ProcessNameHelper
    {
        private static readonly ISet<string> KnownTableauServerProcesses = new HashSet<string>
        {
            "backgrounder",
            "clustercontroller",
            "dataserver",
            "filestore",
            "FNPLicensingService",
            "FNPLicensingService64",
            "httpd",
            "hyper",
            "hyperd",
            "lmgrd",
            "postgres",
            "redis-server",
            "searchserver",
            "tabadminagent",
            "tabadmincontroller",
            "tabadminservice",
            "tabadmwrk",
            "tabcmd",
            "tableau",
            "tabprotosrv",
            "tabrepo",
            "tdeserver",
            "tdeserver64",
            "vizportal",
            "vizqlserver",
            "wgserver",
            "zookeeper"
        };

        public static bool IsKnownTableauServerProcess(string processName)
        {
            if (String.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            int indexOfLastPeriod = processName.LastIndexOf(".", StringComparison.InvariantCulture);
            if (indexOfLastPeriod > -1)
            {
                // Trim extension to normalize between Windows & Linux process names
                processName = processName.Substring(0, indexOfLastPeriod);
            }

            return KnownTableauServerProcesses.Contains(processName, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}