using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EasyTCP
{
  
    public class Server
    {
        private bool _running;
        public bool Running
        {
            get
            {
                return _running;
            }
            set
            {
                _running = value;
            }
        }

        public event EventHandler<ClientStateArgs> ClientConnected;
        public event EventHandler<ClientStateArgs> ClientDisconnected;
        public event EventHandler<DataReceivedArgs> DataReceived;
        private TcpListener Listener;
        private CancellationTokenSource _cts;
        public Channels ConnectedChannels;
        
        public Server()
        {
            Listener = new TcpListener(IPAddress.Parse(Globals.ServerAddress), Globals.ServerPort);
        }

        public Server(string ip, int port)
        {
            Listener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public async Task Start()
        {
            try
            {
                Listener.Start();
                Running = true;
                ConnectedChannels = new Channels(this);
                _cts = new CancellationTokenSource();
                while (Running)
                {
                    var client = await Listener.AcceptTcpClientAsync(_cts.Token);
                    _= Task.Run(() => new Channel(this).Open(client));
                }

            }
            catch(SocketException)
            {
                throw;
            }
            catch(ChannelRegistrationException)
            {
                throw;
            }
            catch(OperationCanceledException)
            {
                // This happens when Stop() is run
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            Listener.Stop();
            Running = false;
        }

        public void OnClientConnected(ClientStateArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }

        public void OnClientDisconnected(ClientStateArgs e)
        {
            ClientDisconnected?.Invoke(this, e);
        }

        public void OnDataIn(DataReceivedArgs e)
        {
            DataReceived?.Invoke(this, e);
        }
    }
}
