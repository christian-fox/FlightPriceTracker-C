using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace RyanairFlightTrackBot
{
    internal static class FlightTrackerApp
    {

        //private Timer dailyTimer;

        //private void DailyTimer(AppConfig config)
        //{
        //    // Once flight checks have finished, update SCHEDULED_TIME environment variable to tomorrow
        //    ConfigReader.UpdateNextScheduledTime(config);

        //    // Set up the timer to call RunFlightChecks once a day
        //    dailyTimer = new Timer(RunFlightChecks, null, config.NextScheduledTime - DateTime.Now);

        //}

        static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            // Start the host and background service on a separate thread
            Task.Run(() => host.Run());
            host.WaitForShutdown();
            // Call my GUI
            App app = new App();
            app.Run(new MainWindow());
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<BackgroundService>(provider => new BackgroundService());
                });
    }
}
