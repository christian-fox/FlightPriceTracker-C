//using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RyanairFlightTrackBot
{
    internal class DatabaseHandler
    {
        //private ILogger logger = Log.ForContext<Flight>();
        //private static readonly Logger logger = LoggerManager.GetLogger();

        private readonly Flight flight;
        private readonly string tableName;
        // note the _ prefix indicates a class-level field/attrib. This is GOOD PRACTICE!
        private static readonly string _databaseFileName = "FlightBotDataBase.db";
        private static readonly string _connectionString =
            $"Data Source={Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\", _databaseFileName)};"; private bool _tableCreated = false; 

        internal DatabaseHandler(Flight flight)
        {
            this.flight = flight;
            // Table names in the following format: [2 letters]_[4 numbers]_YYYY_MM-DD
            this.tableName = $"{flight.flightNumber.Replace(' ', '_')}_{flight.sFlightDate.Replace('-', '_')}";
            Console.WriteLine(tableName);
        }

        internal static void InitialiseFlightList()
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Retrieve the names of all tables in the database
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader["name"].ToString();

                            // Checks the flightNumber format in minimum of either [first 7 chars] or [the whole string if <7 chars]
                            // Note if the tableName is <7 chars, it wont be a match anyway.
                            if (Regex.IsMatch(tableName.Substring(0, Math.Min(7, tableName.Length)), @"^[A-Z]{2}_\d{4}$"))
                            {
                                Flight flight = FlightTableToObject(tableName);
                                if (flight != null)
                                {
                                    // Populate flightList with Flight objects
                                    Flight.flightList.Add(flight);
                                }
                                else
                                {
                                    //throw new Exception(string.Format("Invalid Flight Table"));
                                    LoggerManager.logger.Error("Invalid Flight Table for: ", tableName);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static Flight FlightTableToObject(string tableName)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Retrieve relevant properties/attributes from the table
                    command.CommandText = $"SELECT OriginAirport, DestinationAirport, FlightDate, FlightID, RecipientList FROM {tableName} LIMIT 1";
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Extract values from the database
                            string originAirport = reader["OriginAirport"].ToString();
                            string destinationAirport = reader["DestinationAirport"].ToString();
                            string sFlightDate = reader["FlightDate"].ToString();
                            string flightNumber = reader["FlightID"].ToString();
                            // Extract the recipientList value from the database and split it into a List<string> ------------- assuming recipientList is stored as a comma separated string in the database tables ----------
                            List<string> recipientList = reader["RecipientList"].ToString().Split(',').ToList();                            // Return a new Flight object
                            return new Flight(originAirport, destinationAirport, sFlightDate, flightNumber, recipientList);
                        }
                    }
                }
            }
            // Return null or handle the case where no data is found
            return null;
        }

        public void CreateNewFlightTable()
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Check if the table already exists
                    command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                    if (command.ExecuteScalar() == null)
                    {
                        // Table does not exist, create it --- handle errors where currency/flightID is over specified limits (VARCHAR(10))
                        command.CommandText = $"CREATE TABLE [{tableName}] " +
                            $"(Currency VARCHAR(20), FlightPrice NUMERIC, CurrentDateTime DATETIME NOT NULL UNIQUE, SeatAvailability INTEGER, " +
                            $"FlightID VARCHAR(7) PRIMARY KEY, OriginAirport TEXT NOT NULL, DestinationAirport TEXT NOT NULL, FlightDate DATETIME NOT NULL, FlightTime TEXT, RecipientList TEXT NULL)"; 
                        command.ExecuteNonQuery();
                        _tableCreated = true;
                        // convert flight.recipientList type to string
                        string recipientListStr = flight.recipientList != null ? string.Join(",", flight.recipientList) : null;
                        // Insert data into the newly created table
                        command.CommandText = $"INSERT INTO [{tableName}] (Currency, FlightPrice, CurrentDateTime, SeatAvailability, FlightID, OriginAirport, DestinationAirport, FlightDate, FlightTime, RecipientList) " +
                                              $"VALUES (@Currency, @FlightPrice, @CurrentDateTime, @SeatAvailability, @FlightID, @OriginAirport, @DestinationAirport, @FlightDate, @FlightTime, @RecipientList)";
                        command.Parameters.AddWithValue("@Currency", flight.currency);
                        command.Parameters.AddWithValue("@FlightPrice", flight.flightPrice);
                        command.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);
                        command.Parameters.AddWithValue("@SeatAvailability", flight.seatAvailability);
                        command.Parameters.AddWithValue("@FlightID", flight.flightNumber);
                        command.Parameters.AddWithValue("@OriginAirport", flight.originAirport);
                        command.Parameters.AddWithValue("@DestinationAirport", flight.destinationAirport);
                        command.Parameters.AddWithValue("@FlightDate", flight.flightDate);
                        command.Parameters.AddWithValue("@FlightTime", flight.sFlightTime);
                        // If recipientListStr is null, use DBNull.Value as the parameter value
                        command.Parameters.AddWithValue("@RecipientList", recipientListStr != null ? (object)recipientListStr : DBNull.Value);
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            // Handle the exception (e.g., log it, throw a custom exception, etc.)
                            LoggerManager.logger.Warn( $"Failed to append Database record for flight {flight.flightNumber} (in CreateFlightTable()). EXCEPTION: {ex}");
                            Console.WriteLine($"Exception: {ex}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets flightPrice from last row/record in the table
        /// </summary>
        /// <returns>float flight price, or null</returns>
        public void GetLastFlightPrice()
        {
            // Handle exception where table was created just now, CreateFlightTable() will add the initial values from today
            if (_tableCreated)
            {
                LoggerManager.logger.Info($"Flight {tableName} has just been created today, therefore not data for yesterdays/previous price.");
                return;
            }
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(connection))
                    {
                        command.CommandText = command.CommandText =
                            $"SELECT FlightPrice, CurrentDateTime FROM [{tableName}] ORDER BY CurrentDateTime DESC LIMIT 1";
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                flight.previousPrice = reader.GetDouble(reader.GetOrdinal("FlightPrice"));
                                flight.dtPrevDateAndTime = reader.GetDateTime(reader.GetOrdinal("DateAndTime"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.logger.Warn($"Flight {flight.flightNumber}, Exception: {ex}.\n " +
                    $"Could not access SQL Database to obtain prevoiusPrice");
            }
        }

        public void AppendRecord()
        {
            // Handle exception where table was created just now, CreateFlightTable() will add the initial values from today
            if (_tableCreated) return;
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = $"INSERT INTO [{tableName}] (Currency, FlightPrice, CurrentDateTime, SeatAvailability) " +
                        $"VALUES (@Currency, @FlightPrice, @CurrentDateTime, @SeatAvailability)";
                    command.Parameters.AddWithValue("@Currency", flight.currency);
                    command.Parameters.AddWithValue("@FlightPrice", flight.flightPrice);
                    command.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@SeatAvailability", flight.seatAvailability);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception (e.g., log it, throw a custom exception, etc.)
                        LoggerManager.logger.Warn($"Failed to append Database record for flight {flight.flightNumber}. EXCEPTION: {ex}");
                        Console.WriteLine($"Exception: {ex}");
                    }
                }
            }
        }


    }
}
