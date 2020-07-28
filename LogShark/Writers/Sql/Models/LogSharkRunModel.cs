using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogShark.Writers.Sql.Models
{
    [Table("logshark_runs")]
    public class LogSharkRunModel
    {
        public const string RunSummaryIdColumnName = "logshark_run_id"; 
        
        public string RunId { get; set; }
        public DateTime StartTimestamp { get; set; }
        public string LogSetLocation { get; set; }
    }
}
