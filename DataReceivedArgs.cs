
using System;
using System.Linq;

namespace EasyTCP
{
    
    public class DataReceivedArgs : EventArgs, IDisposable
    {
        public string ConnectionId { get; set; }
        public byte[] Data { get; set; } = new byte[2048];
        public Channel ThisChannel { get; set; }

        public int ReceivedDataSize { get; set; }

        public void Dispose()
        {
            ((IDisposable)ThisChannel).Dispose();
        }
    }
}
