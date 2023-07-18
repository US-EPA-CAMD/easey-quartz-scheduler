using System.Collections.Generic;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	public class QaEvaluationRequest : EvaluationRequest {
		public List<string> qaCertEventId{ get; set;}
		public List<string> testExtensionExemptionId { get; set; }
		public List<string> testSumId { get; set; }
	}
}