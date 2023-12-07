using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RyanairFlightTrackBot
{
    internal static class FlightTrackerApp
    {

        static readonly string operatingSystem = "Windows"; // Use "MAC" for Mac
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
            using IHost host = CreateHostBuilder(args).Build();
            host.Run();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<BackgroundService>(provider => new BackgroundService());
                });
    }
}
