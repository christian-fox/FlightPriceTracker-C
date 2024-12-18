using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;

namespace RyanairFlightTrackBot
{
    internal static class FlightTrackerApp
    {
        static readonly string operatingSystem = "Windows"; // Use "MAC" for Mac
        internal static void RunNewFlightCheck(Flight flight, LoadingWindow loadingWindow)
        {
            // Initialise the logger on each entry of the background checks -- need to initialise a logger when adding a new flight too!
            LoggerManager.InitialiseLogger();

            // Create new instance of webScraper object that corresponds to the specific flight
            WebScraper webScraper = new WebScraper(flight, loadingWindow);
            webScraper.GetFlightPrice(operatingSystem);
            if (flight.flightPriceStr == null)
            {
                // If flight price is not obtained, could be an invalid flight.
                //EmailNotifier.NotifyMissingPriceBug(); -------- dont really want to email the Developer every time a flight is entered incorrectly. ----------- create a loading/progress bar on the GUI and return an error msg if invalid.
                
                return;
            }
            // Creating an instance of the FileHandler class for this specific flight
            FileHandler fileHandler = new FileHandler(flight);
            DatabaseHandler databaseHandler = new DatabaseHandler(flight);
            // Check if flight table exists in Database, if not create it 
            databaseHandler.CreateNewFlightTable();
            // Write flight price to file
            fileHandler.StoreFlightDetails(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff"));

            // Valid flight. Add to object list to run in background.
            Flight.flightList.Add(flight);
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Initialisation: Create flight objects from tables
            DatabaseHandler.InitialiseFlightList();

            // Run GUI on the main thread
            App app = new App();
            MainWindow mainWindow = new MainWindow();
            //// Handle the Closed event to gracefully shut down the host
            //mainWindow.Closed += async (sender, e) =>
            //{
            //    // Shut down the IHost when the main window is closed
            //    //host?.StopAsync().Wait();
            //    await host.StopAsync();
            //};
            app.Run(mainWindow);

            // Start the host and background service on a separate thread
            Task.Run(() =>
            {
                IHost host = CreateHostBuilder(args).Build();
                host.Run();
            });
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<BackgroundService>(provider => new BackgroundService());
                });
    }
}
