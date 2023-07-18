using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("email_to_process", Schema = "camdecmpsaux")]
	public class EmailToProcess
	{
 		[Key]
		[Column("to_process_id")]
 		public long ProcessId { get; set; }

		[Column("fac_id")]
 		public decimal FacId { get; set; }

        [Column("email_type")]
 		public string EmailType { get; set; }

		[Column("event_code")]
 		public int EventCode { get; set; } 

		[Column("userid")]
 		public string? UserId { get; set; } 

		[Column("mon_plan_id")]
 		public string MonPlanId { get; set; } 

		[Column("rpt_period_id")]
 		public decimal RptPeriodId { get; set; }

		[Column("em_sub_access_id")]
 		public long EmSubAccessId { get; set; }

		[Column("submission_type")]
 		public string SubmissionType { get; set; }

		[Column("is_mats")]
 		public System.Nullable<bool> IsMats { get; set; }

		[Column("context")]
 		public string Context { get; set; }

		[Column("status_cd")]
 		public string StatusCode { get; set; }
	}
}
