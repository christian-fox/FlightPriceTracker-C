using Microsoft.SqlServer.Server;
using RyanairFlightTrackBot;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private void OnTextBoxClicked(object sender, MouseButtonEventArgs e) 
        {
            if (sender is TextBox textBox)
            {
                textBox.Text = string.Empty;
                //textBox.PreviewMouseLeftButtonDown -= OnTextBoxClicked; // Remove the event handler after the first click    ////////////// do i want to do this anymore???

                // Subscribe to LostFocus event
                textBox.LostFocus += TextBoxLostFocusHandler;
            }
        }

        private void TextBoxLostFocusHandler(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Check if the text box is empty
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // Repopulate the original text
                    if (textBox.Name == "DepartureAirportTextBox")
                    {
                        textBox.Text = "Blackpool";
                    }
                    else if (textBox.Name == "DestinationAirportTextBox")
                    {
                        textBox.Text = "Alicante";
                    }
                    else if (textBox.Name == "DateTextBox")
                    {
                        textBox.Text = "YYYY-MM-DD";
                    }
                    else if (textBox.Name == "FlightNumberTextBox")
                    {
                        textBox.Text = "FR 1234";
                    }
                    else if (textBox.Name == "NotifyEmailsTextBox")
                    {
                        textBox.Text = "name1@domain.com, ...";
                    }
                }
                // Unsubscribe from LostFocus event
                textBox.LostFocus -= TextBoxLostFocusHandler;
            }
        }
        /// <summary>
        /// Need to include some error handling here. For example: spaces/commas in emailList, flightID starting with FR or RK, dateStr in correct format, valid airports...
        /// </summary>
        private void TrackButton_Click(object sender, RoutedEventArgs e)
        {
            // Show Loading Window
            LoadingWindow loadingWindow = new LoadingWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            loadingWindow.Show();

            // Check user-formatting of Textboxes
            bool validFlight = TextBoxValidityCheck(sender, e);
            if (!validFlight) return;

            // Retrieve values from text boxes
            string departureAirport = DepartureAirportTextBox.Text;
            string destinationAirport = DestinationAirportTextBox.Text;
            string dateStr = DateTextBox.Text;
            string flightNumber = FlightNumberTextBox.Text;
            List<string> emailList = new List<string>(NotifyEmailsTextBox.Text.Split(','));

            // Create an instance of the Flight class
            Flight newFlight = new Flight(departureAirport, destinationAirport, dateStr, flightNumber, emailList);

            FlightTrackerApp.RunNewFlightCheck(newFlight);
            // ---------------------------------------------------------------------------------------------------
            // Create a loading/progress bar at bottom & display error msg for invalid flight details ("Flight not found").
            // ---------------------------------------------------------------------------------------------------
            //



            // Simulate completion of checkpoints (replace this with your actual logic)
            ThreadPool.QueueUserWorkItem(state =>
            {
                // Simulate checkpoint 1 completion
                Thread.Sleep(1000);
                // Raise an event or call a method to indicate completion of checkpoint 1
                loadingWindow.Dispatcher.Invoke(() => loadingWindow.HandleCheckpointCompletion());
            });



            //// For now, let's display a message box with the flight details
            //MessageBox.Show($"New Flight Created:\n{newFlight}", "Flight Tracking");
        }

        /// <summary>
        /// Returns true for valid flight info.
        /// </summary>
        private bool TextBoxValidityCheck(object sender, RoutedEventArgs e)
        {
            bool validFlight = true;
            string errorMsg = string.Empty;

            // Retrieve values from text boxes
            // Handle digits in Destination Airport error
            if (DepartureAirportTextBox.Text.Any(char.IsDigit))
            {
                validFlight = false;
                errorMsg = errorMsg + "\nInvalid Departure Airport";
            }
            // Handle digits in Destination Airport error
            if (DestinationAirportTextBox.Text.Any(char.IsDigit))
            {
                validFlight = false;
                errorMsg = errorMsg + "\nInvalid Destination Airport";
            }
            // Handle Date formatting error
            DateTime date;
            if (!DateTime.TryParseExact(DateTextBox.Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date))
            {
                validFlight = false;
                errorMsg = errorMsg + "\nInvalid Date format. Use the format: YYYY-MM-DD";
            }
            else if (date < DateTime.Now.Date)
            {
                validFlight = false;
                errorMsg = errorMsg + "\nDate must be in the future.";
            }
            else if (date >= DateTime.Now.Date.AddYears(1).AddDays(1)) 
            { 
                validFlight = false; 
                errorMsg = errorMsg + "\nDate must be within a year from today."; 
            }
            // Flight number must be in a specific format ([2 letters]][space][4 numbers])
            if (!Regex.IsMatch(FlightNumberTextBox.Text, @"^[A-Z]{2}\s\d{4}$"))
            {
                validFlight = false;
                errorMsg = errorMsg + "\nInvalid format for Flight Number";
            }
            if (!validFlight)
            {
                MessageBox.Show(errorMsg, "Invalid Flight Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return validFlight;
        }


        internal MainWindow()
        {
            InitializeComponent();

            // Set up logger
            //Log.Logger = new LoggerConfiguration()
            //    .WriteTo.Console()
            //    .CreateLogger();
            // Using logger class now
            //LoggerManager.InitialiseLogger();

            //RunFlightChecks();
        }
    }
}
