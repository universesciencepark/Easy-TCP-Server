
using System;
using System.Linq;

namespace EasyTCP
{
    
    public class ClientStateArgs : EventArgs, IDisposable
    {
        public string ConnectionId { get; set; }
        public Channel ThisChannel { get; set; }

        public void Dispose()
        {
            ((IDisposable)ThisChannel).Dispose();
        }
    }
}
