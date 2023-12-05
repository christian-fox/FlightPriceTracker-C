using System;

namespace RyanairFlightTrackBot
{

    internal class AppConfig
    {
        internal DateTime NextScheduledTime { get; set; }
        internal string GmailPassword { get; set; }
    }


    internal class ConfigReader
    {
        internal static AppConfig ReadConfig()
        {
            AppConfig config = new AppConfig();

            try
            {
                // Read environment variables
                string scheduledTimeString = Environment.GetEnvironmentVariable("SCHEDULED_TIME");
                string gmailPassword = Environment.GetEnvironmentVariable("GMAIL_PASSWORD");

                // Convert to DateTime if needed
                config.NextScheduledTime = DateTime.Parse(scheduledTimeString);
                config.GmailPassword = gmailPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading environment variables: {ex.Message}");
                // Handle the error as needed
                //
                // End programme ? Environment.Exit(0);  ???
                //

            }

            return config;
        }
    }
