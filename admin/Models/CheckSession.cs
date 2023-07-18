using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("check_session", Schema = "camdecmpswks")]
	public class CheckSession
	{
 		[Key]
		[Column("chk_session_id")]
 		public string Id { get; set; } 

		[Column("mon_plan_id")]
 		public string MonitorPlanId { get; set; }

		[Column("severity_cd")]
 		public string SeverityCode { get; set; }
	}
}
