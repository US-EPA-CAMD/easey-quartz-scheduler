using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("check_session", Schema = "camdecmpswks")]
	public class CheckSession
	{
 		[Key]
		[Column("check_session_id")]
 		public int Id { get; set; } 

		[Column("mon_plan_id")]
 		public string MonitorPlanId { get; set; }

		[Column("severity_code")]
 		public string SeverityCode { get; set; }
	}
}