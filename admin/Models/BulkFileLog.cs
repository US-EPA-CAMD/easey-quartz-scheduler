using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("bulk_file_log", Schema = "camdaux")]
	public class BulkFileLog
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
 		public decimal? Year { get; set; }

        [Column("quarter")]
 		public decimal? Quarter { get; set; }

        [Column("state_cd")]
 		public string StateCd { get; set; }

        [Column("prg_cd")]
 		public string PrgCd { get; set; }
	}
}
