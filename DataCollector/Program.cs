using Quartz;
using Quartz.Impl;
using System;
using System.Threading.Tasks;

namespace DataCollector
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            // Grab the Scheduler instance from the Factory
            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();

            // and start it off
            await scheduler.Start();

            // define the job 
            var job = JobBuilder.Create<DataCollectionJob>()
                .WithIdentity("DataCollectionJob")
                .Build();

            // Trigger the job
            var trigger = TriggerBuilder.Create()
                .WithIdentity("DataCollectionJobTrigger")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(1)
                    .RepeatForever())
                .Build();

            // schedule the job 
            await scheduler.ScheduleJob(job, trigger);

            while (true)
            {
                await Task.Delay(5);
            }
        }
    }
}
