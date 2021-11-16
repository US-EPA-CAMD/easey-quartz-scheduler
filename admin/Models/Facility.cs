using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("plant", Schema = "camd")]
	public class Facility
	{
 		[Key]
		[Column("fac_id")]
 		public int Id { get; set; }

		[Column("oris_code")]
 		public int OrisCode { get; set; }		 

		[Column("facility_name")]
		public string Name { get; set; }
	}
}
