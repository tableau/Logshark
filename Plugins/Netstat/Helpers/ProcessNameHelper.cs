using System;
using System.Collections.Generic;
using System.Linq;

namespace Logshark.Plugins.Netstat.Helpers
{
    internal static class ProcessNameHelper
    {
        private static readonly IList<string> KnownTableauServerProcesses = new List<string>
        {
            "backgrounder.exe",
            "clustercontroller.exe",
            "dataserver.exe",
            "filestore.exe",
            "FNPLicensingService.exe",
            "FNPLicensingService64.exe",
            "httpd.exe",
            "lmgrd.exe",
            "postgres.exe",
            "redis-server.exe",
            "searchserver.exe",
            "tabadminservice.exe",
            "tabadmwrk.exe",
            "tableau.exe",
            "tabprotosrv.exe",
            "tabrepo.exe",
            "tdeserver.exe",
            "tdeserver64.exe",
            "vizportal.exe",
            "vizqlserver.exe",
            "wgserver.exe",
            "zookeeper.exe"
        };

        public static bool IsKnownTableauServerProcess(string processName)
        {
            return KnownTableauServerProcesses.Contains(processName, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}