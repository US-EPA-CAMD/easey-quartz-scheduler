using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("evaluation_queue", Schema = "camdecmpsaux")]
	public class Evaluation
	{
 		[Key]
		[Column("evaluation_id")]
 		public Int64 EvaluationId { get; set; }

        [Column("evaluation_set_id")]
 		public string EvaluationSetId { get; set; }

		[Column("process_cd")]
 		public string ProcessCode { get; set; }

        [Column("test_sum_id")]
 		public string TestSumId { get; set; }

        [Column("qa_cert_event_id")]
 		public string QaCertEventId { get; set; }

        [Column("test_extension_exemption_id")]
 		public string TeeId { get; set; }

        [Column("rpt_period_id")]
 		public Int32? RptPeriod { get; set; }

        [Column("severity_cd")]
 		public string ServerityCode { get; set; }

		[Column("submitted_on")]
 		public DateTime SubmittedOn { get; set; }

		[Column("status_cd")]
 		public string StatusCode { get; set; }

		[Column("details")]
 		public string Details { get; set; }
	}
}
