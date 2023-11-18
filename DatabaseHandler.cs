using CsvHelper.Configuration.Attributes;
using Serilog;
using System;
using System.Data;
using System.Data.SQLite;

namespace RyanairFlightTrackBot
{
    internal class DatabaseHandler
    {
        private ILogger logger = Log.ForContext<Flight>();


        private Flight flight;
        private readonly string tableName;
        private bool _tableCreated = false; // note the _ prefix indicates a class-level field/attrib. This is GOOD PRACTICE!

        internal DatabaseHandler(Flight flight)
        {
            this.flight = flight;
            this.tableName = $"{flight.sFlightDate.Replace('-', '_')}_{flight.flightNumber.Replace(' ', '_')}";
            Console.WriteLine(tableName);
        }


        private string _connectionString = @"Data Source=C:/Users/chris/source/repos/RyanairFlightTrackBot/FlightBotDataBase.db;";
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
                            $"FlightID TEXT PRIMARY KEY NOT NULL, OriginAirport TEXT NOT NULL, DestinationAirport TEXT NOT NULL, FlightDate DATETIME NOT NULL, FlightTime TEXT)"; 
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
                            logger.Warning($"Failed to append Database record for flight {flight.flightNumber} (in CreateFlightTable()). EXCEPTION: {ex}");
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
                logger.Information($"Flight {tableName} has just been created today, therefore not data for yesterdays/previous price.");
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
                logger.Warning($"Flight {flight.flightNumber}, Exception: {ex}.\n " +
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
                        logger.Warning($"Failed to append Database record for flight {flight.flightNumber}. EXCEPTION: {ex}");
                        Console.WriteLine($"Exception: {ex}");
                    }
                }
            }
        }


    }
}
