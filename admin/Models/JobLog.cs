using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("job_log", Schema = "camdaux")]
	public class JobLog
	{
 		[Key]
		[Column("job_id")]
 		public Guid JobId { get; set; }

		[Column("job_system")]
 		public string JobSystem { get; set; }

		[Column("job_class")]
 		public string JobClass { get; set; }

        [Column("job_name")]
 		public string JobName { get; set; }

		[Column("add_date")]
 		public DateTime AddDate { get; set; } 

		[Column("start_date")]
 		public DateTime? StartDate { get; set; } 

		[Column("end_date")]
 		public DateTime? EndDate { get; set; } 

		[Column("status_cd")]
 		public string StatusCd { get; set; }

		[Column("additional_details")]
 		public string AdditionalDetails { get; set; }
	}
}
