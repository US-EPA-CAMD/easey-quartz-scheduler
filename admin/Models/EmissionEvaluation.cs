using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("emission_evaluation", Schema = "camdecmpswks")]
	public class EmissionEvaluation
	{
		[Key]
		[Column("mon_plan_id")]
 		public string MonPlanId { get; set; }

		[Key]
        [Column("rpt_period_id")]
 		public Int32 RptPeriod { get; set; }

		[Column("eval_status_cd")]
 		public string EvalStatus { get; set; }
	}
}
