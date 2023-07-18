using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("monitor_location", Schema = "camdecmpswks")]
	public class MonitorLocation
	{
 		[Key]
		[Column("mon_loc_id")]
 		public string Id { get; set; }

		[Column("unit_id")]
		public int? UnitId { get; set; }

		[Column("unitid")]
		public string UnitName { get; set; }		

		[Column("stack_pipe_id")]
 		public string StackPipeId { get; set; }

		[Column("stack_name")]
 		public string StackName { get; set; }
	}
}
