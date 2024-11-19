using System;
using MySql.Data.MySqlClient;
using System.Speech.Synthesis;

namespace Queuing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify a mode: display, kiosk, or teller.");
                return;
            }

            string mode = args[0].ToLower();
            string connectionString = "Server=localhost; Database=QueueSystem; User Id=root; Password=Carvs@10072000;";

            switch (mode)
            {
                case "display":
                    RunDisplay(connectionString);
                    break;
                case "kiosk":
                    RunKiosk(connectionString);
                    break;
                case "teller":
                    RunTeller(connectionString);
                    break;
                default:
                    Console.WriteLine("Invalid mode specified. Please use display, kiosk, or teller.");
                    break;
            }
        }

        static void RunDisplay(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connected to the database.");
                SpeechSynthesizer synthesizer = new SpeechSynthesizer();
                int lastNumber = -1;

                while (true)
                {
                    var command = new MySqlCommand("SELECT Number FROM QueueNumbers WHERE Status = 'Serving' ORDER BY Id DESC LIMIT 1", connection);
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        int currentNumber = reader.GetInt32(0);
                        if (currentNumber != lastNumber)
                        {
                            Console.Clear();
                            Console.WriteLine("Now Serving: " + currentNumber);
                            synthesizer.SpeakAsync("Now serving number " + currentNumber);
                            lastNumber = currentNumber;
                        }
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("No numbers are currently being served.");
                    }
                    reader.Close();
                    System.Threading.Thread.Sleep(1000); // Refresh every second
                }
            }
        }

        static void RunKiosk(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("Press Enter to take a number...");
                    Console.ReadLine();

                    // Get the next number
                    var getMaxNumberCommand = new MySqlCommand("SELECT IFNULL(MAX(Number), 0) + 1 FROM QueueNumbers", connection);
                    int nextNumber = Convert.ToInt32(getMaxNumberCommand.ExecuteScalar());

                    // Insert the new number
                    var insertCommand = new MySqlCommand("INSERT INTO QueueNumbers (Number, Status) VALUES (@Number, 'Waiting')", connection);
                    insertCommand.Parameters.AddWithValue("@Number", nextNumber);
                    insertCommand.ExecuteNonQuery();

                    Console.Clear();
                    Console.WriteLine("Your number is: " + nextNumber);
                }
            }
        }

        static void RunTeller(string connectionString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                int? lastCalledId = null;
                SpeechSynthesizer synthesizer = new SpeechSynthesizer();

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("Press Enter to call the next number or type 'recall' to recall the last number...");
                    string input = Console.ReadLine();

                    if (input.ToLower() == "recall")
                    {
                        if (lastCalledId.HasValue)
                        {
                            // Recall the last called number
                            var getLastCalledNumberCommand = new MySqlCommand("SELECT Number FROM QueueNumbers WHERE Id = @Id", connection);
                            getLastCalledNumberCommand.Parameters.AddWithValue("@Id", lastCalledId.Value);
                            var lastCalledNumber = getLastCalledNumberCommand.ExecuteScalar();

                            if (lastCalledNumber != null)
                            {
                                Console.Clear();
                                Console.WriteLine("Recalling number: " + lastCalledNumber);
                                synthesizer.SpeakAsync("Recalling number " + lastCalledNumber);
                            }
                            else
                            {
                                Console.Clear();
                                Console.WriteLine("No number to recall.");
                            }
                        }
                        else
                        {
                            Console.Clear();
                            Console.WriteLine("No number to recall.");
                        }
                    }
                    else
                    {
                        // Get the next number's Id
                        var getNextIdCommand = new MySqlCommand("SELECT Id FROM QueueNumbers WHERE Status = 'Waiting' ORDER BY Id ASC LIMIT 1", connection);
                        var nextId = getNextIdCommand.ExecuteScalar();

                        if (nextId != null)
                        {
                            // Update the status of the next number to 'Serving'
                            var updateCommand = new MySqlCommand("UPDATE QueueNumbers SET Status = 'Serving' WHERE Id = @Id", connection);
                            updateCommand.Parameters.AddWithValue("@Id", nextId);
                            updateCommand.ExecuteNonQuery();

                            lastCalledId = Convert.ToInt32(nextId);
                            Console.Clear();
                            Console.WriteLine("Called number with Id: " + nextId);
                        }
                        else
                        {
                            Console.Clear();
                            Console.WriteLine("No numbers are currently waiting.");
                        }
                    }
                }
            }
        }
    }
}