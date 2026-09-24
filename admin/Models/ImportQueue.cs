using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  [Table("import_queue", Schema = "camdecmpsaux")]
  public class ImportQueue
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("import_id")]
    public long ImportId { get; set; }

    [Column("import_set_id")]
    public string ImportSetId { get; set; }

    [Column("mon_plan_id")]
    public string MonPlanId { get; set; }

    [Column("file_name")]
    public string FileName { get; set; }

    [Column("temp_s3_bucket_file_path")]
    public string TempS3BucketFilePath { get; set; }

    [Column("file_type_cd")]
    public string FileTypeCode { get; set; }

    [Column("oris_code")]
    public decimal? OrisCode { get; set; }

    [Column("rpt_period_id")]
    public decimal? RptPeriodId { get; set; }

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
