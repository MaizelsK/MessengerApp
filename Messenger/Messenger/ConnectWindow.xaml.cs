using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Messenger
{
    public partial class ConnectWindow : Window
    {
        private TcpClient client;
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private int port;
        private IPAddress ipAddress;

        public ConnectWindow(TcpClient client)
        {
            InitializeComponent();

            this.client = client;
        }

        private async void ConnectBtnClick(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (ValidateForm())
            {
                try
                {
                    await client.ConnectAsync(ipAddress, port);
                    Close();
                }
                catch (SocketException ex)
                {
                    ErrorText.Text = ex.Message;
                }
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(PortText.Text)
                || string.IsNullOrWhiteSpace(IpAddressText.Text))
            {
                ErrorText.Text = "Заполните все поля!";
                return false;
            }

            if (!int.TryParse(PortText.Text, out port))
            {
                ErrorText.Text = "Некорректный порт!";
                return false;
            }

            if (!IPAddress.TryParse(IpAddressText.Text, out ipAddress))
            {
                ErrorText.Text = "Некорректный ip адрес!";
                return false;
            }

            return true;
        }
    }
}
