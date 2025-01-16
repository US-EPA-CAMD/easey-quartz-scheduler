using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("job_configuration", Schema = "camdaux")]
  public class JobConfiguration
  {
    [Key]
    [Column("job_type")]
    public string JobType { get; set; }

    [Column("job_name")]
    public string JobName { get; set; }

    [Column("job_description")]
    public string JobDescription { get; set; }

    [Column("job_group")]
    public string JobGroup { get; set; }

    [Column("trigger_name")]
    public string TriggerName { get; set; }

    [Column("trigger_description")]
    public string TriggerDescription { get; set; }

    [Column("cron_expression")]
    public string CronExpression { get; set; }

    [Column("active")]
    public bool IsActive { get; set; }

    [Column("run_once")]
    public bool RunOnce { get; set; }

    [Column("run_at")]
    public DateTime? RunAt { get; set; }
  }
}
