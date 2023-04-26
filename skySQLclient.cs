using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SQLiteClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string server = "localhost";
            int port = 1200;

            try
            {
                using (TcpClient client = new TcpClient(server, port))
                {
                    using (SslStream sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate))
                    {
                        try
                        {
                            sslStream.AuthenticateAsClient(server);

                            Console.WriteLine("Connected to server");

                            using (StreamReader reader = new StreamReader(sslStream))
                            {
                                using (StreamWriter writer = new StreamWriter(sslStream))
                                {
                                    Console.Write("Enter username: ");
                                    string username = Console.ReadLine();

                                    Console.Write("Enter password: ");
                                    string password = Console.ReadLine();

                                    writer.WriteLine($"{username}:{password}");
                                    writer.Flush();

                                    string response = reader.ReadLine();

                                    if (response == "Authenticated")
                                    {
                                        Console.WriteLine("Authenticated successfully");

                                        while (true)
                                        {
                                            Console.Write("Enter query (or 'exit' to quit): ");
                                            string query = Console.ReadLine();

                                            if (query == "exit")
                                            {
                                                break;
                                            }

                                            writer.WriteLine(query);
                                            writer.Flush();

                                            response = reader.ReadLine();

                                            Console.WriteLine(response);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Authentication failed");
                                    }
                                }
                            }
                        }
                        catch (AuthenticationException ex)
                        {
                            Console.WriteLine($"Authentication error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
            }
        }

        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            Console.WriteLine($"Certificate error: {sslPolicyErrors}");

            return false;
        }
    }
}
