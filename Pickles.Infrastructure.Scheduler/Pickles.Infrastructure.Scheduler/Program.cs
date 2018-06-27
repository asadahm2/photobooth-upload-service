using Atlas;
using Autofac;
using Pickles.Infrastructure.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Scheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration =
                Host.UseAppConfig<JobService>()
                    .AllowMultipleInstances()
                    .WithRegistrations(b => b.RegisterModule(new AutofacModule()))
                    .WithArguments(args);

            Host.Start(configuration);

        }
    }
}
