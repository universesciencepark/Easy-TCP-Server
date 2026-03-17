
using System;
using System.Linq;

namespace EasyTCP
{
    
    public class ClientStateArgs : EventArgs
    {
        public string ConnectionId { get; set; }
        public Channel ThisChannel { get; set; }
    }
}
