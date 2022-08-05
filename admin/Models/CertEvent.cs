using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("qa_cert_event", Schema = "camdecmpswks")]
	public class CertEvent
	{
 		[Key]
		[Column("qa_cert_event_id")]
 		public string Id { get; set; }

		[Column("eval_status_cd")]
 		public string EvalStatus { get; set; }

		[Column("chk_session_id")]
 		public string CheckSessionId { get; set; }
	}
}
