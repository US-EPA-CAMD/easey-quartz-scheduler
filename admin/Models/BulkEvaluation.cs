using System.Collections.Generic;

namespace Epa.Camd.Quartz.Scheduler.Models
{
    public class EvaluationItem{
        public string monPlanId {get; set;}
        public bool submitMonPlan {get; set;}
        public List<string> testSumIds {get; set; }
        public List<string> qceIds {get; set; }
        public List<string> teeIds {get; set; }
        public List<string> emissionReportingPeriods {get; set; }
    }

	public class BulkEvaluationRequest {
		public List<EvaluationItem> items {get; set;}
        public string UserId { get; set; }
		public string UserEmail { get; set; }
	}
}