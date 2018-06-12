using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public NetworkStream networkStream;

        public string ClientName;
        private bool isFileAttached;
        private string filePath;
        private const int packetSize = 1024;

        public ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MainWindow()
        {
            InitializeComponent();

            ChatListBox.ItemsSource = messages;
            NewConnection();

            // Приветствие пользователя
            Greeting greeting = new Greeting(this);
            greeting.ShowDialog();

            Task.Run(() => { HandleClient(); });
        }

        #region Обработка входящих и исходящих сообщений

        // Обработка пользователя
        private void HandleClient()
        {
            try
            {
                while (true)
                {
                    Message message = GetResponce();

                    if (message != null)
                    {
                        if (!message.IsDownloadRequest)
                        {
                            message.Sender += ": ";

                            Dispatcher.Invoke(() =>
                            {
                                messages.Add(message);
                            });
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Получения ответа от сервера
        private Message GetResponce()
        {
            Message message;

            int bytesRead;
            int allBytesRead = 0;
            byte[] messageBytes = new byte[packetSize];

            try
            {
                bytesRead = networkStream.Read(messageBytes, 0, packetSize);

                // Получения сообщения и метаданных файла из первых 1024 байт
                string messageJson = Encoding.Default.GetString(messageBytes);
                message = JsonConvert.DeserializeObject<Message>(messageJson);

                if (message != null && message.IsDownloadRequest)
                {
                    byte[] fileData = new byte[message.Metadata.FileSize];
                    int bytesLeft = (int)message.Metadata.FileSize;

                    // Получения самого файла пакетами в 1024 байта
                    while (bytesLeft > 0)
                    {
                        int nextPacketSize = (bytesLeft > packetSize) ? packetSize : bytesLeft;

                        bytesRead = networkStream.Read(fileData, allBytesRead, nextPacketSize);
                        allBytesRead += bytesRead;
                        bytesLeft -= bytesRead;
                    }

                    FileHelper.SaveFile(message.Metadata, fileData);
                }
            }
            catch (IOException ex)
            {
                throw ex;
            }
            catch (InvalidOperationException ex)
            {
                throw ex;
            }

            return message;
        }

        // Отправка сообщения
        private void SendMessage(Message message)
        {
            byte[] data;

            if (isFileAttached)
            {
                data = new byte[message.Metadata.FileSize + 1024];

                byte[] fileData;
                using (FileStream fs = File.OpenRead(message.Metadata.FilePath))
                {
                    fileData = new byte[message.Metadata.FileSize];
                    fs.Read(fileData, 0, (int)message.Metadata.FileSize);
                }

                Array.Copy(fileData, 0, data, packetSize, fileData.Length);
            }
            else
                data = new byte[packetSize];

            // Вставка сообщения и метаданных в начало массива всех данных
            byte[] messageJson = Encoding.Default.GetBytes(JsonConvert.SerializeObject(message));
            Array.Resize(ref messageJson, packetSize);
            Array.Copy(messageJson, data, packetSize);

            int bytesSent = 0;
            int bytesLeft = data.Length;

            try
            {
                while (bytesLeft > 0)
                {
                    int nextPacketSize = (bytesLeft > packetSize) ? packetSize : bytesLeft;

                    networkStream.Write(data, bytesSent, nextPacketSize);
                    bytesSent += nextPacketSize;
                    bytesLeft -= nextPacketSize;
                }
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

        // Запрос на скачивание файла
        private void RequestFile(FileMetadata metadata)
        {
            Message fileRequest = new Message
            {
                IsDownloadRequest = true,
                Metadata = metadata
            };

            string jsonRequest = JsonConvert.SerializeObject(fileRequest);
            byte[] requestData = Encoding.Default.GetBytes(jsonRequest);

            try
            {
                networkStream.Write(requestData, 0, requestData.Length);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Работа с соеденением

        // Закрытие текущего соеденения
        private void CloseConnection()
        {
            // Отправка сообщения серверу о том
            // что клиент отключился
            SendMessage(new Message
            {
                Sender = ClientName,
                IsDisconnect = true
            });

            networkStream.Close();
            client.Close();
            client = new TcpClient();
        }

        // Вызов окна нового соеденения
        private void NewConnection()
        {
            ConnectWindow connectWindow = new ConnectWindow(client);
            connectWindow.ShowDialog();

            if (client.Connected)
            {
                networkStream = client.GetStream();
                Task.Run(() => { HandleClient(); });
            }
        }

        #endregion

        #region Обработка нажатий кнопок

        // Подготовка сообщения
        private void SendBtnClick(object sender, RoutedEventArgs e)
        {
            string messageText = MessageTextBox.Text;

            // Если нет ни сообщения, ни прикрепленного файла
            // Отмена отправки
            if (string.IsNullOrWhiteSpace(messageText)
                && isFileAttached == false)
                return;

            Message newMessage = new Message
            {
                Sender = ClientName,
                MessageText = !string.IsNullOrWhiteSpace(messageText) ?
                                messageText : null
            };

            if (isFileAttached)
            {
                newMessage.IsFileAttached = isFileAttached;
                newMessage.Metadata = FileHelper.GetFileMetadata(filePath);
            }

            try
            {
                SendMessage(newMessage);
                ClearUI();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Прикрепление файла
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

        // Смена сервера
        private void ChangeServerClick(object sender, RoutedEventArgs e)
        {
            CloseConnection();

            Dispatcher.Invoke(() =>
            {
                ClearUI(true);
                NewConnection();
            });
        }

        // Обработка нажатия скачивания файла
        private void DownloadBtnClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var dataContext = button.DataContext;
            Message selectedMessage = dataContext as Message;

            RequestFile(selectedMessage.Metadata);
        }

        #endregion

        // При закрытии приложения
        protected override void OnClosing(CancelEventArgs e)
        {
            CloseConnection();
        }

        // Сброс UI элементов
        private void ClearUI(bool isServerChange = false)
        {
            if (isServerChange)
            {
                messages = new ObservableCollection<Message>();
                ChatListBox.ItemsSource = messages;
            }

            MessageTextBox.Text = "";
            isFileAttached = false;
            FileNameBlock.Text = "";
        }
    }
}