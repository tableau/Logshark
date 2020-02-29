using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Plugins.Tabadmin.Model
{
    public class TableauServerVersion
    {
        public DateTime? EndDate { get; set; }
        public DateTime? EndDateGmt { get; set; }
        public string Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StartDateGmt { get; set; }
        public string TimestampOffset { get; set; }
        public string Version { get; set; }
        public string VersionLong { get; set; }
        public string Worker { get; set; }

    }
}
