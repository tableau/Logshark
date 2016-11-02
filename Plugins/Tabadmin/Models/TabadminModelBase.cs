using ServiceStack.DataAnnotations;
using System;

namespace Logshark.Plugins.Tabadmin.Models
{
    public class TabadminModelBase
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        public Guid LogsetHash { get; set; }

        [Index(Unique = true)]
        public Guid EventHash { get; set; }
    }
}