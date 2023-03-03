using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	[Table("reporting_period", Schema = "camdecmpsmd")]
	public class ReportingPeriod
	{
 		[Key]
		[Column("rpt_period_id")]
 		public Int32 ReportingPeriodId { get; set; }

		[Column("calendar_year")]
 		public decimal year { get; set; }

		[Column("quarter")]
 		public decimal quarter { get; set; }

		[Column("period_abbreviation")]
 		public string PeriodAbbreviation { get; set; }
	}
}
