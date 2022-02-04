using System;

namespace LogShark.Plugins.TabadminController
{
    public class TabadminControllerBuildRecord
    {
        public DateTimeOffset RoundedTimestamp { get; }
        public string Build { get; }

        public TabadminControllerBuildRecord(DateTimeOffset roundedTimestamp, string build)
        {
            RoundedTimestamp = roundedTimestamp;
            Build = build;
        }
    }
}