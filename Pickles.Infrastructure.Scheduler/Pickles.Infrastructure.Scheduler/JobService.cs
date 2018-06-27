using Atlas;
using Pickles.Infrastructure.UploadJob;
using Quartz;

namespace Pickles.Infrastructure.Scheduler
{
    internal class JobService : IAmAHostedProcess
    {
        const int IntervalInMinutes = 1;

        public IScheduler Scheduler { get; set; }

        public IJobListener AutofacJobListener { get; set; }

        
        #region Implementation of IAmAHostedProcess

        public void Start()
        {   
            Scheduler.ListenerManager.AddJobListener(AutofacJobListener);
            Scheduler.Start();
        }

        public void Stop()
        {
            Scheduler.Shutdown();
        }

        public void Resume()
        {
            Scheduler.ResumeAll();
        }

        public void Pause()
        {
            Scheduler.PauseAll();
        }

        #endregion
    }
}
