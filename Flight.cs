using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media.Media3D;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace RyanairFlightTrackBot
{
    /// <summary>
    /// Class to manage fights
    /// </summary>
    internal class Flight
    {
        private ILogger logger = Log.ForContext<Flight>();

        public static int NoOfObjects { get; private set; } = 0;
        public static int NoOfObjectCreationAttempts { get; private set; } = 0;
        public bool isValid { get; private set; } = false;
        internal string seatAvailability { get; set; } = null;
        internal string flightPriceStr { get; set; } = null;
        internal double? flightPrice { get; set; } = null;
        internal string sFlightTime { get; set; } = null;
        internal double? previousPrice { get; set; } = null;
        internal string currency { get; set; } = null;
        internal string sPrevDateAndTime { get; set; } = null;
        internal DateTime? dtPrevDateAndTime { get; set; } = null;

        internal string originAirport { get; }
        internal string destinationAirport { get; }
        internal string sFlightDate { get; }
        internal string flightNumber { get; }
        internal List<string> recipientList { get; }
        internal DateTime flightDate;
        internal string month = null;
        internal int day = 0;
        internal string fileName;
        internal static List<Flight> flightList;// = new List<Flight>();

        /// <summary>
        /// Flight object constructor
        /// </summary>
        public Flight(string originAirport, string destinationAirport, string sFlightDate, string flightNumber, List<string> recipientList = null)
        {
            NoOfObjectCreationAttempts++;
            // Class attributes
            this.originAirport = originAirport;
            this.destinationAirport = destinationAirport;
            this.flightNumber = flightNumber;
            this.recipientList = recipientList;
            //this.logger = logger;                  // attempting to NOT pass logger as an input arg in c#, rather initialising it at the top of the class. 
            this.sFlightDate = sFlightDate;

            try
            {
                // Check that the input string is in the correct format
                if (!DateTime.TryParseExact(sFlightDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    throw new ArgumentException("Incorrect date format, should be YYYY-MM-DD");
                }

                // Parse the string into a datetime object
                flightDate = DateTime.ParseExact(sFlightDate, "yyyy-MM-dd", null);

                // Check that flightDate is within the bounds dateTime.Now+1 =< flightDate < dateTime.Now+365 (days)   -- (these <, >, = limits should be checked on the website)
                if (flightDate < DateTime.Now.AddDays(1))
                {
                    throw new ArgumentException("Flight date must be in the future");
                }
                else if (flightDate >= DateTime.Now.AddYears(1).AddDays(1))
                {
                    throw new ArgumentException("Flight date must be within a year");
                }

                // Extract month and day from the flightDate datetime object
                month = (flightDate.Month == 9) ? "Sept" : flightDate.ToString("MMM");
                day = flightDate.Day;

                // Also a class attribute
                fileName = $"{flightNumber.Replace(' ', '_')}_{sFlightDate}.csv";

                // Upon successful object instantiation, add 1 to the NoOfObjects count
                NoOfObjects++;
                isValid = true;
            }
            catch (Exception e)
            {
                // invalid date format - delete object instance
                logger.Error(e.Message);                                    // note, removed 'this.' from logger
                Console.WriteLine(e.Message);
                // del self (not applicable in C#)
                isValid = false;
            }
            //AddValidFlight(flight);
        }

        /// <summary>
        /// Provides a human-readable string representation of the Flight object. Used for debugging and logging.
        /// </summary>
        /// <returns>A string sentence describing the flight object.</returns>
        public override string ToString()
        {
            return $"Flight {flightNumber} from {originAirport} to {destinationAirport} on {month} {day} with recipients {recipientList}.";
        }

        /// <summary>
        /// Adds valid flight to flightList ----------- already adding flight to flightlist upon creation --- all flights will be in this list, not just valid ones
        /// </summary>
        //private void AddValidFlight(Flight flight)
        //{
        //    if (flight.isValid) flightList.Add(flight);
        //}
            
        /// <summary>
        /// Allows other classes to access the list of valid flights, flightList
        /// </summary>
        /// <returns>flightList</returns>
        internal static List<Flight> GetFlightList()
        {
            return flightList;
        }

        /// <summary>
        /// Checks if the WebScraper.GetFlightPrice was successful in getting each flights price.
        /// </summary>
        /// <returns>True if the list of valid flights has 1+ null flightPrice value</returns>
        public static Boolean hasNullPrices()
        {
            bool hasNullPrices = false;
            foreach (Flight flight in flightList)
            {
                if (flight.flightPriceStr == null)
                {
                    hasNullPrices = true;
                    break;
                }
            }
            return hasNullPrices;
        }

        /// <summary>
        /// Checks whether the flight price today is > £3 less than the price yesterday (previous price in the file/table).
        /// </summary>
        /// <returns></returns>
        internal Boolean PriceDecreased()
        {
            Boolean priceDecreased = false;

            // Handle exception case where previousPrice was not obtained.
            // Note, this exception is currently handled twice (in main run flight ckeck loop too)
            if (previousPrice == null) return priceDecreased;
            else if (previousPrice - flightPrice > 3) priceDecreased = true;
            else Console.WriteLine($"Flight {flightNumber} price has not decreased since last check.");
            return priceDecreased;
        }

        /// <summary>
        /// Extracts currency & flightPrice (double type) from flight.flightPriceStr
        /// </summary>
        internal void ParseFlightPriceStr()
        {
            // 'Capturing Group' Patterns:
            //   -  ([^\d]*) matches any character that is not a digit
            //   -  (\d+\.\d+): \d+ matches one or more digits and \. matches a literal dot
            Regex regex = new Regex(@"^([^\d]*)(\d+\.\d+)$");

            Match match = regex.Match(flightPriceStr);
            if (match.Success)
            {
                currency = match.Groups[1].Value;
                flightPrice = Math.Round(double.Parse(match.Groups[2].Value), 2);
            }
            else
            {
                // Handle the case when the regex doesn't match
                Console.WriteLine("Price format not recognized");
                logger.Warning($"Flight {flightNumber} flightPriceStr not in correct format.");
            }
        }

    }
}
