using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("cors_config", Schema = "camdaux")]
	public class CorsOptions
	{
 		[Key]
		[Column("cors_id")]
 		public int Id { get; set; } 

		[Column("key")]
 		public string Key { get; set; }

		[Column("value")]
 		public string Value { get; set; }
	}
}
