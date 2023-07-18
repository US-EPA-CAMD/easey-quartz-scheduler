using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("eval_status_code", Schema = "camdecmpsmd")]
	public class EvalStatusCode
	{
 		[Key]
		[Column("eval_status_cd")]
 		public string Code { get; set; }

		[Column("eval_status_cd_description")]
 		public string Description { get; set; }
	}
}
