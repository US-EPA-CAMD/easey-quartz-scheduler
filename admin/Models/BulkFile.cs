using System;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class BulkFile
  {
    public string Filename { get; set; }
    public string S3Path { get; set; }
    public string DataType { get; set; }
    public string DataSubType { get; set; }
    public string Grouping { get; set; }
    public long Bytes { get; set; }
    public long KiloBytes { get { return this.Bytes / 1024; } }
    public long MegaBytes { get { return this.KiloBytes / 1024; } }
    public long GigaBytes { get { return this.MegaBytes / 1024; } }
    public DateTime LastUpdated { get; set; }
    public string Description { get; set; }
  }
}