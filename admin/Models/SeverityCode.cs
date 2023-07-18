using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("severity_code", Schema = "camdecmpsmd")]
	public class SeverityCode
	{
 		[Key]
		[Column("severity_cd")]
 		public string Code { get; set; }

		[Column("eval_status_cd")]
 		public string EvalStatusCode { get; set; }

		[Column("severity_cd_description")]
 		public string Description { get; set; }
	}
}
