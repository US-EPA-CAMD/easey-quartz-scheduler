using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("emission_evaluation", Schema = "camdecmpswks")]
	[PrimaryKey(nameof(MonPlanId), nameof(RptPeriod))]
	public class EmissionEvaluation
	{
		[Column("mon_plan_id")]
 		public string MonPlanId { get; set; }

        [Column("rpt_period_id")]
 		public Int32 RptPeriod { get; set; }

		[Column("chk_session_id")]
 		public string CheckSessionId { get; set; }

		[Column("eval_status_cd")]
 		public string EvalStatus { get; set; }
	}
}
