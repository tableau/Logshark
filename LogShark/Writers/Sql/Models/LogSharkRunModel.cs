using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LogShark.Writers.Sql.Models
{
    [Table("logshark_runs")]
    public class LogSharkRunModel
    {
        public string RunId { get; set; }
        public DateTime StartTimestamp { get; set; }
        public string LogSetLocation { get; set; }
    }
}
