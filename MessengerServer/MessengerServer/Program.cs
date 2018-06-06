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

        private static List<TcpClient> clients = new List<TcpClient>();

        static void Main(string[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(ipAddress, defaultPort);
            server.Start();

            try
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    //Socket handler = server.Accept();

                    clients.Add(client);

                    Task.Run(() => { HandleClient(client); });
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }

            server.Stop();
        }

        // Обработка клиента
        static void HandleClient(TcpClient client)
        {
            try
            {
                ShowNewMember(client);

                while (true)
                {
                    Message response = GetResponse(client);

                    if (response != null)
                    {
                        string message = JsonConvert.SerializeObject(response);
                        SendToAllClients(message);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }

            CloseClient(client);
        }

        // Уведомление о новом подключении
        private static void ShowNewMember(TcpClient client)
        {
            try
            {
                Message response = GetResponse(client);

                if (response != null)
                {
                    Message newConnectionMessage = new Message
                    {
                        Sender = "Server",
                        MessageText = $"{response.Sender} подключился к серверу"
                    };

                    Console.WriteLine(newConnectionMessage.MessageText);

                    string newClientMessage = JsonConvert.SerializeObject(newConnectionMessage);
                    SendToAllClients(newClientMessage);

                    string clientMessage = JsonConvert.SerializeObject(response);
                    SendToAllClients(clientMessage);
                }
            }
            catch (SocketException ex)
            {
                throw ex;
            }
        }


        // Получения ответа от клиента
        private static Message GetResponse(TcpClient client)
        {
            Message message;

            try
            {
                using (var stream = client.GetStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string response = reader.ReadToEnd();

                        message = JsonConvert.DeserializeObject<Message>(response);
                    }
                }

                return message;
            }
            catch (SocketException ex)
            {
                throw ex;
            }
        }

        // Рассылка сообщения всем клиентам
        private static void SendToAllClients(string message)
        {
            foreach (TcpClient client in clients)
            {
                try
                {
                    using (var stream = client.GetStream())
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(message);
                        }
                    }
                }
                catch(SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // Закрытие клиента
        private static void CloseClient(TcpClient client)
        {
            clients.Remove(client);
            client.Close();
        }
    }
}
