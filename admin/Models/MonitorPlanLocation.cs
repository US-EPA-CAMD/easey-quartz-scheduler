using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Easey.JobScheduler.Models
{
	[Table("monitor_plan_location", Schema = "camdecmpswks")]
	public class MonitorPlanLocation
	{
 		[Key]
		[Column("monitor_plan_location_id")]
 		public string Id { get; set; }

		[Column("mon_plan_id")]
		public string PlanId { get; set; }

		[Column("mon_loc_id")]
 		public string LocationId { get; set; }
	}
}
