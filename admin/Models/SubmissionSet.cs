using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("submission_set", Schema = "camdecmpsaux")]
  public class SubmissionSet
  {
    [Key]
    [Column("submission_set_id")]
    public string SetId { get; set; }

    [Column("mon_plan_id")]
    public string MonPlanId { get; set; }

    [Column("queued_time")]
    public DateTime QueuedTime { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("fac_id")]
    public Int32 FacId { get; set; }

    [Column("oris_code")]
    public Int32 OrisCode { get; set; }

    [Column("fac_name")]
    public string FacName { get; set; }

    [Column("configuration")]
    public string Config { get; set; }

    [Column("activity_id")]
    public string ActivityId { get; set; }

    [Column("status_cd")]
    public string StatusCode { get; set; }

    [Column("started_time")]
    public DateTime? StartedTime { get; set; }

    [Column("completed_time")]
    public DateTime? CompletedTime { get; set; }

    [Column("note")]
    public string Note { get; set; }

    [Column("note_time")]
    public DateTime? NoteTime { get; set; }
  }
}
