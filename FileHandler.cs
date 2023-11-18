using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;

namespace RyanairFlightTrackBot
{
    internal class FileHandler
    {
        private ILogger logger = Log.ForContext<Flight>();


        private Flight flight;

        internal FileHandler(Flight flight)
        {
            this.flight = flight;
        }

        /// <summary>
        /// Need to separate the functionality of creating new file and storing detials, as in DatabaseHandler
        /// </summary>
        /// <param name="dateTime">Current DateTime as of calling the function</param>
        public void StoreFlightDetails(string dateTimeStr)
        {
            using (var writer = new StreamWriter(flight.fileName, true))
            {
                string formattedCurrency = flight.currency == "€" ? "EUR" : flight.currency;
                string[] rowData = { formattedCurrency, flight.flightPrice.ToString(), dateTimeStr, flight.seatAvailability };
                string formattedRow = string.Join(",", rowData);

                writer.WriteLine(formattedRow);
            }

            // Logging or additional actions if needed
            logger.Information($"Flight_Price = {flight.flightPrice}");
        }


        //// Method to write flight price to file named 'flight.fileName'
        //// Note, pass in dateTime at the time of calling the procedure, not at the time the flight object is created.
        //public void StoreFlightDetails(Flight flight, DateTime dateTime)
        //{
        //    try
        //    {
        //        using (StreamWriter writer = new StreamWriter(flight.fileName, true, Encoding.GetEncoding("ISO-8859-15")))
        //        using (CsvWriter csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
        //        {
        //            Match match = Regex.Match(flight.flightPriceStr, @"^([^\d]*)(\d+\.\d+)$");
        //            if (match.Success)
        //            {
        //                flight.currency = match.Groups[1].Value;
        //                flight.flightPrice = Math.Round(float.Parse(match.Groups[2].Value), 2);
        //            }

        //            if (flight.currency == "€")
        //            {
        //                csvWriter.WriteRecords(new List<FlightRecord>
        //            {
        //                new FlightRecord
        //                {
        //                    Currency = "EUR",
        //                    FlightPrice = flight.flightPrice,
        //                    DateTime = dateTime,
        //                    SeatAvailability = flight.seatAvailability
        //                }
        //            });
        //            }
        //            else
        //            {
        //                csvWriter.WriteRecords(new List<FlightRecord>
        //            {
        //                new FlightRecord
        //                {
        //                    Currency = flight.currency,
        //                    FlightPrice = flight.flightPrice,
        //                    DateTime = dateTime,
        //                    SeatAvailability = flight.seatAvailability
        //                }
        //            });
        //            }
        //        }

        //        this.logger.Information($"Flight_Price = {flight.flightPrice}");
        //    }
        //    catch (Exception e)
        //    {
        //        this.logger.Error($"Error writing to file: {e.Message}");
        //    }
        //}

        // Method to get previous (yesterday's) price, date, and time from the 'this.fileName' file
        
        /// <summary>
        /// Gets last/final price in the table and sets flight.previousPrice to this value.
        /// </summary>
        public void GetPreviousPrice()
        {
            try
            {
                string[] lines = File.ReadAllLines(flight.fileName, Encoding.GetEncoding("ISO-8859-15"));

                // Check if there are any rows in the file
                if (lines.Length > 0)
                {
                    // Get the last row
                    string lastRow = lines[lines.Length - 1];

                    // Check if the last row has a non-empty DateTime value (3rd column)
                    string[] lastRowColumns = lastRow.Split(',');
                    if (lastRowColumns.Length > 2 && !string.IsNullOrEmpty(lastRowColumns[2]))
                    {
                        flight.sPrevDateAndTime = lastRowColumns[2];
                        flight.previousPrice = Math.Round(float.Parse(lastRowColumns[1]), 2);
                        this.logger.Information($"Previous_Price = {flight.previousPrice}");
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.Error($"Error reading from file: {e.Message}");
            }
        }
    }
}
