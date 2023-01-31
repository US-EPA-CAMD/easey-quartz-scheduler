using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Epa.Camd.Quartz.Scheduler.Models
{
    public class EmmisionsEvaluationRequest : EvaluationRequest
    {
       public int RptPeriodId { get; set; }
    }
}
