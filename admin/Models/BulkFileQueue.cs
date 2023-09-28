using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("bulk_file_queue", Schema = "camdaux")]
	public class BulkFileQueue
	{
 		[Key]
		[Column("job_id")]
 		public Guid JobId { get; set; }

		[Column("parent_job_id")]
 		public Guid? ParentJobId { get; set; }

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

		[Column("year")]
 		public int? Year { get; set; }

		[Column("quarter")]
 		public int? Quarter { get; set; }

		[Column("state_cd")]
 		public string StateCode { get; set; }

		[Column("data_type")]
 		public string DataType { get; set; }

		[Column("sub_type")]
 		public string SubType { get; set; }

		[Column("url")]
 		public string Url { get; set; }

		[Column("file_name")]
 		public string FileName { get; set; }

		[Column("program_cd")]
 		public string ProgramCode { get; set; }

		[Column("additional_details")]
 		public string AdditionalDetails { get; set; }
	}
}
