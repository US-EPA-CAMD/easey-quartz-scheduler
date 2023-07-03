using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("email_to_send", Schema = "camdecmpsaux")]
	public class EmailToSend
	{
 		[Key]
		[Column("to_send_id")]
 		public long SendId { get; set; }

		[Column("to_email")]
 		public string ToEmail { get; set; }

        [Column("from_email")]
 		public string FromEmail { get; set; }

		[Column("template_id")]
 		public long TemplateId { get; set; } 

		[Column("context")]
 		public string Context { get; set; } 

		[Column("status_cd")]
 		public string StatusCode { get; set; } 
	}
}
