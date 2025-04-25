using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{

    [Table("pdem_report", Schema = "camdecmpsaux")]
    public class PdemReport
    {
        [Key]
        [Column("pdem_report_id")]
        public long PdemReportId { get; set; }

        [Column("mon_plan_id")]
        public string MonPlanId { get; set; }

        [Column("rpt_period_id")]
        public Int32 RptPeriod { get; set; }

        [Column("submission_id")]
        public long SubmissionId { get; set; }
    }

}
