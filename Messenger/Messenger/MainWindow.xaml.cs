using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Messenger
{
    public partial class MainWindow : Window
    {
        public TcpClient client = new TcpClient();
        public string ClientName;

        private bool isFileAttached;
        private string filePath;

        public ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();
            ChatListBox.ItemsSource = messages;

            ConnectWindow connectWindow = new ConnectWindow(client);
            connectWindow.ShowDialog();

            Greeting greeting = new Greeting(this);
            greeting.ShowDialog();

            Task.Run(() => { HandleClient(); });
        }

        #region Обработка входящих сообщений
        private void HandleClient()
        {
            try
            {
                while (true)
                {
                    Message message = GetResponce();

                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }

            CloseConnection();
        }

        private Message GetResponce()
        {
            Message message;

            try
            {
                using (var stream = client.GetStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string response = reader.ReadToEnd();

                        message = JsonConvert.DeserializeObject<Message>(response);
                    }
                }
            }
            catch (SocketException ex)
            {
                throw ex;
            }

            return message;
        }
        #endregion

        // Смена сервера
        private void ChangeServerClick(object sender, RoutedEventArgs e)
        {
            CloseConnection();

            ConnectWindow connectWindow = new ConnectWindow(client);
            connectWindow.ShowDialog();
        }

        // Закрытие текущего соеденения
        private void CloseConnection()
        {
            client.Close();
            client = new TcpClient();

            //socket.Shutdown(SocketShutdown.Both);
            //socket.Close();
        }

        // Отправка сообщения
        private void SendBtnClick(object sender, RoutedEventArgs e)
        {
            string messageText = MessageTextBox.Text;

            // Если нет ни сообщения, ни прикрепленного файла
            // Отмена отправки
            if (string.IsNullOrWhiteSpace(messageText)
                && isFileAttached == false)
                return;

            if (!string.IsNullOrWhiteSpace(messageText))
            {
                using (var stream = client.GetStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        Message newMessage = new Message
                        {
                            Sender = ClientName,
                            MessageText = messageText
                        };

                        string json = JsonConvert.SerializeObject(newMessage);
                        writer.Write(json);
                    }
                }
            }

            if (isFileAttached)
            {
                using (var stream = client.GetStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        Message newMessage = new Message
                        {
                            IsFileAttached = isFileAttached
                        };

                        string json = JsonConvert.SerializeObject(newMessage);
                        writer.Write(json);
                    }
                }

                SendFile();
            }
        }

        private void SendFile()
        {
            FileMetadata metadata = GetFileMetadata();

            byte[] data = new byte[metadata.FileSize + 512];

            // Вставка метаданных в начало массива всех данных
            byte[] metadataJson = Encoding.Default.GetBytes(JsonConvert.SerializeObject(metadata));
            Array.Resize(ref metadataJson, 512);
            Array.Copy(metadataJson, data, 512);

            byte[] fileData;
            using (FileStream fs = File.OpenRead(metadata.FilePath))
            {
                fileData = new byte[metadata.FileSize];
                fs.Read(fileData, 0, (int)metadata.FileSize);
            }

            Array.Copy(fileData, 0, data, 512, fileData.Length);

            int bufferSize = 1024;
            int bytesSent = 0;
            int bytesLeft = data.Length;

            using (NetworkStream networkStream = client.GetStream())
            {
                while (bytesLeft > 0)
                {

                    int nextPacketSize = (bytesLeft > bufferSize) ? bufferSize : bytesLeft;

                    networkStream.Write(data, bytesSent, nextPacketSize);
                    bytesSent += nextPacketSize;
                    bytesLeft -= nextPacketSize;
                }
            }
        }

        private FileMetadata GetFileMetadata()
        {
            FileInfo fileInfo = new FileInfo(filePath);

            FileMetadata metadata = new FileMetadata
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FilePath = filePath
            };

            return metadata;
        }

        private void AttachFileBtnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                filePath = fileDialog.FileName;
                isFileAttached = true;

                FileNameBlock.Text = filePath;
            }
        }
    }
}