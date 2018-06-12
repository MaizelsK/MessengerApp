using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer
{
    class Program
    {
        private const int defaultPort = 3535;
        private static TcpListener server;

        static void Main(string[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(ipAddress, defaultPort);
            server.Start();

            try
            {
                while (true)
                {
                    TcpClient recievedClient = server.AcceptTcpClient();

                    Client newClient = new Client
                    {
                        TcpClient = recievedClient,
                        Stream = recievedClient.GetStream()
                    };

                    ClientHandler handler = new ClientHandler(newClient);
                    Task.Run(() => { handler.HandleClient(newClient); });
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }

            server.Stop();
        }
    }
}
