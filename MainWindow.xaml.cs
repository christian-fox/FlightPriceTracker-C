﻿using RyanairFlightTrackBot;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RyanairFlightTrackBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Set current working directory
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        // Configure the logging module
        // Note: Logging configuration is not directly converted as it depends on your specific logging requirements

        string operatingSystem = "Windows"; // Use "MAC" for Mac


        public void RunFlightChecks()
        {
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
        }


        private void OnTextBoxClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Text = string.Empty;
                textBox.PreviewMouseLeftButtonDown -= OnTextBoxClicked; // Remove the event handler after the first click
            }
        }

        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve values from text boxes
            string departureAirport = DepartureAirportTextBox.Text;
            string destinationAirport = DestinationAirportTextBox.Text;
            string date = DateTextBox.Text;
            string flightNumber = FlightNumberTextBox.Text;
            List<string> emailList = new List<string>(NotifyEmailsTextBox.Text.Split(','));

            // Create an instance of the Flight class
            Flight newFlight = new Flight(departureAirport, destinationAirport, date, flightNumber, emailList);

            // Now, you can use the 'newFlight' object as needed
            // For example, you might want to perform further actions or store it in your application
            // ...

            // For now, let's display a message box with the flight details
            MessageBox.Show($"New Flight Created:\n{newFlight.ToString()}", "Flight Tracking");
        }


        public MainWindow()
        {
            InitializeComponent();

            // Set up logger
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            RunFlightChecks();
        }
    }
}