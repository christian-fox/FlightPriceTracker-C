using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RyanairFlightTrackBot
{
    internal static class FlightTrackerApp
    {

        static readonly string operatingSystem = "Windows"; // Use "MAC" for Mac
        private Timer dailyTimer;
        //private DateTime nextScheduledTime;

        //static FlightTrackerApp()
        //{
        //    // Retrieve the last scheduled time from persistent storage
        //    // For simplicity, let's assume it's stored in a configuration file or a database
        //    nextScheduledTime = UpdateScheduledTime();

        //    // Calculate the time until the next daily task
        //    TimeSpan timeUntilDailyTask = nextScheduledTime - DateTime.Now;

        //    // Set up the timer to call YourBackgroundServiceMethod once a day
        //    dailyTimer = new Timer(RunFlightChecks, null, timeUntilDailyTask, TimeSpan.FromDays(1));
        //}

        //private DateTime UpdateScheduledTime()
        //{
        //    // Retrieve the next scheduled time from persistent storage
        //    // For simplicity, let's assume it's stored in a configuration file or a database
        //    // Replace this with your actual retrieval logic
        //    return DateTime.Today.AddDays(1).AddHours(8);
        //}
        //}

        private void DailyTimer(AppConfig config)
        {
            // Once flight checks have finished, update SCHEDULED_TIME environment variable to tomorrow
            ConfigReader.UpdateNextScheduledTime(config);

            // Set up the timer to call RunFlightChecks once a day
            dailyTimer = new Timer(RunFlightChecks, null, config.NextScheduledTime - DateTime.Now);

        }

        private static void RunFlightChecks()
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
            DailyTimer(config);
        }

        [STAThread]
        internal static async Task Main()
        {
            // Read Batch file
            AppConfig config = ConfigReader.ReadConfig();

            // Call RunFlightChecks immediately upon application start
            await RunFlightChecksAsync(config);

            // Set up the timer to call RunFlightChecks once a day
            DailyTimer(config);

            // Call my GUI
            App app = new App();
            MainWindow mainWindow = new MainWindow();
            app.Run(mainWindow);
        }

        private static async Task RunFlightChecksAsync(AppConfig config)
        {
            await Task.Run(() => RunFlightChecks(config));
        }


    }
}
