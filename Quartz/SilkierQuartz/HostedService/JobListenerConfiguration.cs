using Quartz;
using System;

namespace SilkierQuartz.HostedService
{
    internal class JobListenerConfiguration
    {
        public JobListenerConfiguration(Type listenerType, IMatcher<JobKey>[] matchers)
        {
            ListenerType = listenerType;
            Matchers = matchers;
        }

        public Type ListenerType { get; }
        public IMatcher<JobKey>[] Matchers  {  get;  }
    }
}