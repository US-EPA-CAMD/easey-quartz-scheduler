using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("job_configuration", Schema = "camdaux")]
  public class JobConfiguration
  {
    [Key]
    [Column("job_class")]
    public string JobClass { get; set; }

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
