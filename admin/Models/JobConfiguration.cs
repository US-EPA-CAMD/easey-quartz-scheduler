using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// TODO: Finish annotating.
namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("job_configurations", Schema = "camdaux")]
  public class JobConfiguration
  {
    public string JobName { get; set; }
    public string JobDescription { get; set; }
    public string JobGroup { get; set; }
    public string JobType { get; set; }
    public string TriggerName { get; set; }
    public string TriggerDescription { get; set; }
    public string CronExpression { get; set; }
    public bool IsActive { get; set; }
  }
}
