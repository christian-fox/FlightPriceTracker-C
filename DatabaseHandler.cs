//using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace RyanairFlightTrackBot
{
    internal class DatabaseHandler
    {
        //private ILogger logger = Log.ForContext<Flight>();
        //private static readonly Logger logger = LoggerManager.GetLogger();

        private readonly Flight flight;
        private readonly string tableName;
        private static readonly string _connectionString = @"Data Source=C:/Users/chris/source/repos/RyanairFlightTrackBot/FlightBotDataBase.db;";
        private bool _tableCreated = false; // note the _ prefix indicates a class-level field/attrib. This is GOOD PRACTICE!

        internal DatabaseHandler(Flight flight)
        {
            this.flight = flight;
            this.tableName = $"{flight.sFlightDate.Replace('-', '_')}_{flight.flightNumber.Replace(' ', '_')}";
            Console.WriteLine(tableName);
        }

        internal static void CreateFlightList()
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

                            // Assuming your tables have names like "FR_XXXX" where XXXX is the flight number
                            if (tableName.StartsWith("FR_"))
                            {
                                Flight flight = CreateFlightFromTable(tableName);
                                if (flight != null)
                                {
                                    // Populate flightList with Flight objects
                                    Flight.flightList.Add(CreateFlightFromTable(tableName));
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

        internal static Flight CreateFlightFromTable(string tableName)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Retrieve relevant properties/attributes from the table
                    command.CommandText = $"SELECT originAirport, destinationAirport, sFlightDate, flightNumber FROM {tableName} LIMIT 1";

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Extract values from the database
                            string originAirport = reader["originAirport"].ToString();
                            string destinationAirport = reader["destinationAirport"].ToString();
                            string sFlightDate = reader["sFlightDate"].ToString();
                            string flightNumber = reader["flightNumber"].ToString();

                            // Extract the recipientList value from the database and split it into a List<string> ------------- assuming recipientList is stored as a comma separated string in the database tables ----------
                            List<string> recipientList = reader["recipientList"].ToString().Split(',').ToList();

                            // Return a new Flight object
                            return new Flight(originAirport, destinationAirport, sFlightDate, flightNumber, recipientList);
                        }
                    }
                }
            }

            // Return null or handle the case where no data is found
            return null;
        }

        public void CreateFlightTable()
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
                        // Table does not exist, create it
                        command.CommandText = $"CREATE TABLE [{tableName}] " +
                            $"(Currency TEXT, FlightPrice NUMERIC, CurrentDateTime DATETIME NOT NULL UNIQUE, SeatAvailability INTEGER, " +
                            $"FlightID TEXT PRIMARY KEY, OriginAirport TEXT NOT NULL, DestinationAirport TEXT NOT NULL, FlightDate DATETIME NOT NULL, FlightTime TEXT)"; 
                        command.ExecuteNonQuery();
                        _tableCreated = true;

                        // Insert data into the newly created table
                        command.CommandText = $"INSERT INTO [{tableName}] (Currency, FlightPrice, CurrentDateTime, SeatAvailability, FlightID, OriginAirport, DestinationAirport, FlightDate, FlightTime) " +
                                              $"VALUES (@Currency, @FlightPrice, @CurrentDateTime, @SeatAvailability, @FlightID, @OriginAirport, @DestinationAirport, @FlightDate, @FlightTime)";

                        command.Parameters.AddWithValue("@Currency", flight.currency);
                        command.Parameters.AddWithValue("@FlightPrice", flight.flightPrice);
                        command.Parameters.AddWithValue("@CurrentDateTime", DateTime.Now);
                        command.Parameters.AddWithValue("@SeatAvailability", flight.seatAvailability);
                        command.Parameters.AddWithValue("@FlightID", flight.flightNumber);
                        command.Parameters.AddWithValue("@OriginAirport", flight.originAirport);
                        command.Parameters.AddWithValue("@DestinationAirport", flight.destinationAirport);
                        command.Parameters.AddWithValue("@FlightDate", flight.flightDate);
                        command.Parameters.AddWithValue("@FlightTime", flight.sFlightTime);

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
