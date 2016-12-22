using Logshark.Plugins.Tabadmin.Models;
using System.Collections.Generic;

namespace Logshark.Plugins.Tabadmin.Helpers
{
    internal abstract class TSVersionHelper
    {
        /// <summary>
        /// Provided a collection of TSVersion objects and a specific TabadminLoggedItem, return the Id field of the active TSVersion as of
        /// tsLoggedItem.Timestamp.
        /// </summary>
        /// <param name="versionList">A collection of TSVersion objects representing a Tableau Server install history.</param>
        /// <param name="tsLoggedItem">The specific log entry for which to retrieve a version id.</param>
        /// <returns>The Id field of the active version as of tsLoggedItem.Timestamp.</returns>
        public static int? GetTSVersionIdByDate(IEnumerable<TSVersion> versionList, TabadminLoggedItem tsLoggedItem)
        {
            foreach (var version in versionList)
            {
                // The most recent version in the versionList should have a null EndDate.
                if ((version.StartDateGmt <= tsLoggedItem.TimestampGmt) &&
                    ((version.EndDateGmt > tsLoggedItem.TimestampGmt) || version.EndDateGmt == null) &&
                    (version.Worker == tsLoggedItem.Worker))
                {
                    return version.Id;
                }
            }
            return null;
        }
    }
}