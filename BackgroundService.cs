using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace RyanairFlightTrackBot
{
    internal class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        internal static void RunFlightChecks()
        {
            // Initialise the logger on each entry of the background checks -- need to initialise a logger when adding a new flight too!
            LoggerManager.InitialiseLogger();

            // Create list of flight objects
            Flight.flightList = new List<Flight>
                {
                    new Flight("Liverpool", "Lanzarote", "2024-03-14", "FR 6574", new List<string> { "christianlfox@aol.com", "FoxsFlightForecast@europe.com" })
                    // Add other flight objects here
                };

            foreach (Flight flight in Flight.flightList)
            {
                Console.WriteLine(flight);

                // Create new instance of webScraper object that corresponds to the specific flight
                WebScraper webScraper = new WebScraper(flight);
                webScraper.GetFlightPrice(operatingSystem);
                if (flight.flightPriceStr == null)
                {
                    // Go to the next flight
                    continue;
                }

                // Creating an instance of the FileHandler class for this specific flight
                FileHandler fileHandler = new FileHandler(flight);
                DatabaseHandler databaseHandler = new DatabaseHandler(flight);

                // Check if flight table exists in Database, if not create it 
                databaseHandler.CreateFlightTable();

                // Get previous (yesterday's) price, date, and time
                //////////// BOTH CLASSES ARE SETTING flight.previousPrice ////////////
                fileHandler.GetPreviousPrice();
                databaseHandler.GetLastFlightPrice();

                // Write flight price to file
                fileHandler.StoreFlightDetails(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff"));
                databaseHandler.AppendRecord();

                // Continue to the next iteration of the loop if previous data is not obtained
                if (flight.previousPrice == null) continue;

                // Email recipient(s)
                // - if recipient list is not empty
                // - if the price has decreased
                if (flight.recipientList != null && flight.PriceDecreased())
                {
                    EmailNotifier emailNotifier = new EmailNotifier(flight);
                    emailNotifier.NotifyRecipientsOfPriceReduction();
                }

                //Thread.Sleep(5000); // Sleep for 5 seconds -- debugging (to see browser)??
            }

            // Ensure flight price is obtained for each flight
            EmailNotifier.NotifyMissingPriceBug();

            // Initiate infinite loop between DailyTimer() & RunFlightChecks()
            //DailyTimer(config);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Your background logic goes here
                Console.WriteLine("Background service is running...");
                RunFlightChecks();

                // Delay background service for 24 hours
                await Task.Delay(24 * 60 * 60 * 1000, stoppingToken);
            }
        }
    }
}
