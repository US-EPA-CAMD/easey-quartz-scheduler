using System.Collections.Generic;

namespace Epa.Camd.Quartz.Scheduler.Models
{
	public class EvaluationRequest {
		public string UserId { get; set; }
		public string UserEmail { get; set; }		
		public string MonitorPlanId { get; set; }
	}
}