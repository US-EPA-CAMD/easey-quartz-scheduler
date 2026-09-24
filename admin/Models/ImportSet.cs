using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("import_set", Schema = "camdecmpsaux")]
  public class ImportSet
  {
    [Key]
    [Column("import_set_id")]
    public string ImportSetId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("user_email")]
    public string UserEmail { get; set; }

    [Column("queued_time")]
    public DateTime QueuedTime { get; set; }

    [Column("claimed_time")]
    public DateTime? ClaimedTime { get; set; }

    [Column("started_time")]
    public DateTime? StartedTime { get; set; }

    [Column("completed_time")]
    public DateTime? CompletedTime { get; set; }

    [Column("note")]
    public string Note { get; set; }

    [Column("note_time")]
    public DateTime? NoteTime { get; set; }

    // Generated column - read only.
    [Column("status_cd")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string StatusCode { get; set; }
  }
}
