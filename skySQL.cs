using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SqliteServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 1200;

            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("Server started on port {0}.", port);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            Console.WriteLine("Client {0} connected.", client.Client.RemoteEndPoint);

            SslStream sslStream = new SslStream(client.GetStream(), false);
            sslStream.AuthenticateAsServer(GetServerCertificate(), false, SslProtocols.Tls12, true);

            bool isAuthenticated = false;

            try
            {
                while (!isAuthenticated)
                {
                    byte[] usernameBytes = ReadMessage(sslStream);
                    byte[] passwordBytes = ReadMessage(sslStream);

                    string username = Encoding.UTF8.GetString(usernameBytes);
                    string password = Encoding.UTF8.GetString(passwordBytes);

                    isAuthenticated = AuthenticateUser(username, password);

                    if (!isAuthenticated)
                    {
                        SendMessage(sslStream, Encoding.UTF8.GetBytes("Authentication failed. Please try again."));
                    }
                }

                SendMessage(sslStream, Encoding.UTF8.GetBytes("Authentication successful. You may now execute queries."));

                while (sslStream.IsAuthenticated && client.Connected)
                {
                    byte[] queryBytes = ReadMessage(sslStream);

                    string query = Encoding.UTF8.GetString(queryBytes);

                    if (query.ToLower() == "exit" || query.ToLower() == "quit")
                    {
                        break;
                    }

                    string result = ExecuteQuery(query);

                    SendMessage(sslStream, Encoding.UTF8.GetBytes(result));
                }

                Console.WriteLine("Client {0} disconnected.", client.Client.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: {0}", ex.Message);
            }
            finally
            {
                sslStream.Close();
                client.Close();
            }
        }

        static byte[] GetServerCertificate()
        {
            string certificateFile = "server.pfx";
            string certificatePassword = "password";

            X509Certificate2 certificate = new X509Certificate2(certificateFile, certificatePassword);

            return certificate.Export(X509ContentType.Pfx, certificatePassword);
        }

        static byte[] ReadMessage(SslStream sslStream)
        {
            List<byte> bytes = new List<byte>();

            int data = sslStream.ReadByte();

            while (data != -1)
            {
                bytes.Add((byte)data);

                if (sslStream.DataAvailable)
                {
                    data = sslStream.ReadByte();
                }
                else
                {
                    break;
                }
            }

            return bytes.ToArray();
        }

        static void SendMessage(SslStream sslStream, byte[] messageBytes)
        {
            sslStream.Write(messageBytes);
            sslStream.WriteByte(0);
            sslStream.Flush();
        }

        static bool AuthenticateUser(string username, string password)
        {
            string connectionString = "Data Source=database.db;Version=3;Pooling=true;Max Pool Size=100;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open
                string query = "SELECT * FROM users WHERE username = @username AND password = @password";

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", HashPassword(password));

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        static string ExecuteQuery(string query)
        {
            string connectionString = "Data Source=database.db;Version=3;Pooling=true;Max Pool Size=100;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                    {
                        DataSet dataSet = new DataSet();

                        adapter.Fill(dataSet);

                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (DataTable table in dataSet.Tables)
                        {
                            foreach (DataRow row in table.Rows)
                            {
                                foreach (DataColumn column in table.Columns)
                                {
                                    stringBuilder.Append(row[column].ToString());
                                    stringBuilder.Append("\t");
                                }

                                stringBuilder.Append("\n");
                            }
                        }

                        return stringBuilder.ToString();
                    }
                }
            }
        }

        static string HashPassword(string password)
        {
            SHA256 sha256 = SHA256.Create();

            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder stringBuilder = new StringBuilder();

            foreach (byte b in bytes)
            {
                stringBuilder.Append(b.ToString("x2"));
            }

            return stringBuilder.ToString();
        }
    }
}
