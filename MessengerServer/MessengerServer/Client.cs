using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessengerServer
{
    public class Client
    {
        public TcpClient TcpClient { get; set; }
        public NetworkStream Stream { get; set; }

        public void CloseConnection()
        {
            Stream.Close();
            TcpClient.Close();
        }
    }
}
