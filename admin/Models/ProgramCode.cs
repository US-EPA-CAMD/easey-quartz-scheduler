using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("program_code", Schema = "camdmd")]
	public class ProgramCode
	{
 		[Key]
		[Column("prg_cd")]
 		public string Code { get; set; }

		[Column("prg_description")]
 		public string Description { get; set; }
	}
}
