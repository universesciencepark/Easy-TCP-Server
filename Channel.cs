using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace EasyTCP
{
    public class Channel : IDisposable
    {
        private Server thisServer;
        public readonly string Id;
        private TcpClient thisClient;
        private readonly byte[] buffer;
        private NetworkStream stream;
        private volatile bool isOpen;
        private bool disposed;
        private readonly object sendLock = new object();

        public Channel(Server myServer)
        {
            thisServer = myServer;
            buffer = new byte[2048];
            Id = Guid.NewGuid().ToString();
        }

        public void Open(TcpClient client)
        {
            thisClient = client;
            isOpen = true;
            if(!thisServer.ConnectedChannels.OpenChannels.TryAdd(Id, this))
            {
                isOpen = false;
                client.Close();
                return;
            }

            try
            {
                thisServer.OnClientConnected(new ClientStateArgs() { ConnectionId = Id, ThisChannel = this });

                using (stream = thisClient.GetStream())
                {
                    int position = 0;

                    while(isOpen)
                    {
                        if (clientDisconnected())
                        {
                            break;
                        }

                        try
                        {
                            while ((position = stream.Read(buffer, 0, buffer.Length)) != 0 && isOpen)
                            {
                                var args = new DataReceivedArgs()
                                {
                                    ConnectionId = Id,
                                    ThisChannel = this,
                                    ReceivedDataSize = position
                                };

                                Buffer.BlockCopy(buffer, 0, args.Data, 0, position);

                                thisServer.OnDataIn(args);
                                if(!isOpen) { break; }
                            }

                            // stream.Read returned 0 — graceful disconnect
                            break;
                        }
                        catch (IOException)
                        {
                            // Client disconnected abruptly
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Stream/client was closed (e.g. via Close() from another thread)
                            break;
                        }
                    }
                }
            }
            finally
            {
                thisServer.OnClientDisconnected(new ClientStateArgs() { ConnectionId = Id, ThisChannel = this });
                Close();
            }
        }

        public void Send(byte[] data)
        {
            if (!isOpen) return;
            lock (sendLock)
            {
                if (!isOpen) return;
                stream.Write(data, 0, data.Length);
            }
        }


        public void Close()
        {
            isOpen = false;
            thisServer.ConnectedChannels.OpenChannels.TryRemove(Id, out _);
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                try { stream?.Close(); } catch { }
                try { thisClient?.Close(); } catch { }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool clientDisconnected()
        {
            try
            {
                return (thisClient.Client.Available == 0 && thisClient.Client.Poll(1, SelectMode.SelectRead));
            }
            catch
            {
                return true;
            }
        }
    }
}
