using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger
{
    public class Message
    {
        public string Sender { get; set; }
        public string MessageText { get; set; }
        public bool IsFileAttached { get; set; }
        public bool IsDownloadRequest { get; set; }
        public bool IsDisconnect { get; set; }
        public FileMetadata Metadata { get; set; }
    }
}
