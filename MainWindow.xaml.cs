using RyanairFlightTrackBot;
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