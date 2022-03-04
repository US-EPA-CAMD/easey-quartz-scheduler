using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("qrtz_bulk_data_file_queue", Schema = "camdaux")]
	public class QuartzBulkDataFile
	{
 		[Key]
		[Column("job_id")]
 		public Guid JobId { get; set; }

		[Column("parent_job_id")]
 		public Guid ParentJobId { get; set; }

		[Column("data_type")]
 		public string DataType { get; set; }

		[Column("data_subtype")]
 		public string DataSubType { get; set; } 

		[Column("year")]
 		public decimal Year { get; set; } 

		[Column("quarter")]
 		public decimal Quarter { get; set; } 
		
		[Column("state_cd")]
 		public string StateCd { get; set; } 

		[Column("prg_cd")]
 		public string PrgCd { get; set; } 

		[Column("add_date")]
 		public DateTime AddDate { get; set; } 

		[Column("start_date")]
 		public DateTime? StartDate { get; set; } 

		[Column("end_date")]
 		public DateTime? EndDate { get; set; } 

		[Column("status_cd")]
 		public string StatusCd { get; set; }

		[Column("status_msg")]
 		public string StatusMsg { get; set; }  

		[Column("is_valid")]
 		public string IsValid { get; set; }  
	}
}
