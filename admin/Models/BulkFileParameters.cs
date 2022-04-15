using System;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class BulkFileParameters
  {
    public decimal? Year {get; set; }

    public decimal? Quarter {get; set; }

    public string StateCode {get; set; }

    public string DataType {get; set; }

    public string SubType {get; set; }

    public string Url {get; set; }

    public string FileName {get; set; }

    public string ProgramCode {get; set; }
  }
}