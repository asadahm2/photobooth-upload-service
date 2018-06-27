using Atlas;
using Autofac;
using Pickles.Infrastructure.Logging;
using Pickles.Infrastructure.Scheduler;
using Pickles.Infrastructure.UploadJob;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Core;
using Pickles.Infrastructure.Data;

namespace Pickles.Infrastructure.Scheduler
{
    internal class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //Scheduler
            builder.Register(c =>
            {
                var scheduler = new StdSchedulerFactory().GetScheduler();
                scheduler.JobFactory = new AutofacJobFactory(ContainerProvider.Instance.ApplicationContainer);
                return scheduler;
            });

            //Jobs
            builder.RegisterType<UploadFile>().AsSelf();

            //Job listener   
            builder.Register(c => new AutofacJobListener(ContainerProvider.Instance)).As<IJobListener>();
            
            //Services
            builder.RegisterType<JobService>().As<IAmAHostedProcess>().PropertiesAutowired();

            //Infrastructure
            //builder.RegisterType<HockeyAppLogger>().As<ILogger>().PropertiesAutowired();
            builder.RegisterType<ApplicationInsightsLogger>().As<ILogger>().PropertiesAutowired();
            builder.RegisterType<SqlDB>().As<IData>().PropertiesAutowired();
        }
        
    }
}
