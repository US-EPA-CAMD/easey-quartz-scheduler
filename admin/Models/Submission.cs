using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("submission_queue", Schema = "camdecmpsaux")]
  public class Submission
  {
    [Key]
    [Column("submission_id")]
    public Int64 SubmissionId { get; set; }

    [Column("submission_set_id")]
    public string SetId { get; set; }

    [Column("note")]
    public string Note { get; set; }

    [Column("note_time")]
    public DateTime? NoteTime { get; set; }

    [Column("status_cd")]
    public string StatusCode { get; set; }
  }
}

