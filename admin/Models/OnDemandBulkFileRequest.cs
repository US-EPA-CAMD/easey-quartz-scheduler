using System;
using System.Collections.Generic;

namespace Epa.Camd.Quartz.Scheduler.Models
{
  public class OnDemandBulkFileRequest
  {

    public int? From  {get; set;}
    public int? To {get; set;}
    public int[] Quarters {get; set;}
    public string[] StateCodes {get; set;}
    public string[] SubTypes {get; set;}
    public string[] ProgramCodes {get; set;}
    public bool emissionsCompliance {get; set;}

    public bool allowanceHoldings {get; set;}

  }
}