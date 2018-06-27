using Atlas;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Scheduler
{
    internal class AutofacJobListener : IJobListener
    {
        private readonly IContainerProvider _containerProvider;
        private IUnitOfWorkContainer _container;

        public AutofacJobListener(IContainerProvider containerProvider)
        {
            _containerProvider = containerProvider;
        }

        public void JobToBeExecuted(IJobExecutionContext context)
        {
            _container = _containerProvider.CreateUnitOfWork();
            _container.InjectUnsetProperties(context.JobInstance);
        }

        public void JobExecutionVetoed(IJobExecutionContext context)
        {
            /*noop*/
        }

        public void JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException)
        {
            _container.Dispose();
        }

        public string Name
        {
            get { return "AutofacInjectionJobListener"; }
        }
    }
}
