using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer
{
    public class ClientHandler
    {
        private const int packetSize = 1024;
        private static List<Client> clients = new List<Client>();

        public ClientHandler(Client client)
        {
            clients.Add(client);
        }

        // Обработка клиента
        public void HandleClient(Client client)
        {
            try
            {
                ShowNewMember(client);

                while (true)
                {
                    Message response = GetResponse(client);

                    if (response != null)
                    {
                        if (!response.IsDownloadRequest)
                        {
                            if (response.IsDisconnect)
                            {
                                response.MessageText = response.Sender + " покинул нас...";
                                response.Sender = "Server";
                            }

                            string message = JsonConvert.SerializeObject(response);

                            SendToAllClients(message);
                        }
                        else
                        {
                            Console.WriteLine("Запрос на загурузку файла: " + response.Metadata.FileName);
                            SendFileToClient(response.Metadata, client);
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }

            CloseClient(client);
        }

        // Уведомление о новом подключении
        private void ShowNewMember(Client client)
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
            catch (ObjectDisposedException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (IOException ex)
            {
                throw ex;
            }
        }

        // Получения ответа от клиента
        private Message GetResponse(Client client)
        {
            Message message = null;

            int bytesRead;
            int allBytesRead = 0;

            byte[] messageBytes = new byte[packetSize];

            try
            {
                bytesRead = client.Stream.Read(messageBytes, 0, packetSize);

                // Получения сообщения и метаданных файла из первых 1024 байт
                string messageJson = Encoding.Default.GetString(messageBytes);
                message = JsonConvert.DeserializeObject<Message>(messageJson);

                if (message != null)
                    if (message.IsFileAttached)
                    {
                        byte[] fileData = new byte[message.Metadata.FileSize];
                        int bytesLeft = (int)message.Metadata.FileSize;

                        // Получения самого файла пакетами в 1024 байта
                        while (bytesLeft > 0)
                        {
                            int nextPacketSize = (bytesLeft > packetSize) ? packetSize : bytesLeft;

                            bytesRead = client.Stream.Read(fileData, allBytesRead, nextPacketSize);
                            allBytesRead += bytesRead;
                            bytesLeft -= bytesRead;
                        }

                        SaveFile(message.Metadata, fileData);
                    }
            }
            catch (ObjectDisposedException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }
            catch (IOException ex)
            {
                throw ex;
            }

            return message;
        }

        // Рассылка сообщения всем клиентам
        private void SendToAllClients(string message)
        {
            List<Client> clientsToSend = clients;

            foreach (Client client in clientsToSend)
            {
                try
                {
                    byte[] data = Encoding.Default.GetBytes(message);

                    client.Stream.Write(data, 0, data.Length);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine(ex.Message);
                    CloseClient(client);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex.Message);
                    CloseClient(client);
                }
            }
        }

        // Отправка файла клиенту
        private void SendFileToClient(FileMetadata metadata, Client client)
        {
            byte[] data;
            data = new byte[metadata.FileSize + 1024];

            string filePath = Directory.GetCurrentDirectory() + "\\Saved Files\\" + metadata.FileName;
            byte[] fileData;

            using (FileStream fs = File.OpenRead(filePath))
            {
                fileData = new byte[metadata.FileSize];
                fs.Read(fileData, 0, (int)metadata.FileSize);
            }
            Array.Copy(fileData, 0, data, packetSize, fileData.Length);

            Message sendFileMessage = new Message
            {
                IsDownloadRequest = true,
                Metadata = metadata
            };

            // Вставка сообщения и метаданных в начало массива всех данных
            byte[] messageJson = Encoding.Default.GetBytes(JsonConvert.SerializeObject(sendFileMessage));
            Array.Resize(ref messageJson, packetSize);
            Array.Copy(messageJson, data, packetSize);

            int bytesSent = 0;
            int bytesLeft = data.Length;

            try
            {
                while (bytesLeft > 0)
                {
                    int nextPacketSize = (bytesLeft > packetSize) ? packetSize : bytesLeft;

                    client.Stream.Write(data, bytesSent, nextPacketSize);
                    bytesSent += nextPacketSize;
                    bytesLeft -= nextPacketSize;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // Закрытие клиента
        private void CloseClient(Client client)
        {
            clients.Remove(client);
            client.CloseConnection();
        }

        // Сохранение файла
        private void SaveFile(FileMetadata metadata, byte[] fileData)
        {
            Task.Run(() =>
            {
                string savedFilesPath = Directory.GetCurrentDirectory() + "\\Saved Files";
                Directory.CreateDirectory(savedFilesPath);

                File.WriteAllBytes(savedFilesPath + "\\" + metadata.FileName, fileData);
                Console.WriteLine("Файл " + metadata.FileName + " успешно сохранен");
            });
        }
    }
}