using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Autofac;

namespace Pickles.Infrastructure.Scheduler
{
    internal class AutofacJobFactory : IJobFactory
    {
        private readonly IContainer Container;
        
        public AutofacJobFactory(IContainer container)
        {
            Container = container;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            IJob job = null;

            job = (IJob)Container.Resolve(bundle.JobDetail.JobType);

            return job;
        }

        public void ReturnJob(IJob job)
        {
        }
    }
}
