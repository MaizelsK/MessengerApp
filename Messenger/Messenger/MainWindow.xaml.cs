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
            catch(SocketException ex)
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
        }
    }
}
