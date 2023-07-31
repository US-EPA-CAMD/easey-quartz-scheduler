using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("submission_set", Schema = "camdecmpsaux")]
	public class SubmissionSet
	{
 		[Key]
		[Column("submission_set_id")]
 		public string SetId { get; set; }

        [Column("mon_plan_id")]
 		public string MonPlanId { get; set; }

		[Column("submitted_on")]
 		public DateTime SubmittedOn { get; set; }

        [Column("user_id")]
 		public string UserId { get; set; }

        [Column("user_email")]
 		public string UserEmail { get; set; }

        [Column("fac_id")]
 		public Int32 FacId { get; set; }

        [Column("fac_name")]
 		public string FacName { get; set; }

        [Column("configuration")]
 		public string Config { get; set; }

		[Column("activity_id")]
 		public string ActivityId { get; set; }

		[Column("status_cd")]
 		public string StatusCode { get; set; }

		[Column("details")]
 		public string Details { get; set; }
	}
}
