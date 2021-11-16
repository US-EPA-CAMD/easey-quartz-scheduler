using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("monitor_plan", Schema = "camdecmpswks")]
	public class MonitorPlan
	{
 		[Key]
		[Column("mon_plan_id")]
 		public string Id { get; set; }

		[Column("fac_id")]
		public int FacilityId { get; set; }

		[Column("eval_status_cd")]
 		public string EvalStatus { get; set; }
	}
}
