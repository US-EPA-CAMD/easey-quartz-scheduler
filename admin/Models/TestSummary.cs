using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("test_summary", Schema = "camdecmpswks")]
	public class TestSummary
	{
 		[Key]
		[Column("test_sum_id")]
 		public string Id { get; set; }

		[Column("eval_status_cd")]
 		public string EvalStatus { get; set; }

		[Column("chk_session_id")]
 		public string CheckSessionId { get; set; }
	}
}
