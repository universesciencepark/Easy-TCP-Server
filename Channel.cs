﻿using System;
using System.Collections.Generic;
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
        private bool isOpen;
        private bool disposed;

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
                throw (new ChannelRegistrationException("Unable to add channel to channel list"));
            }

            thisServer.OnClientConnected(new ClientStateArgs() { ConnectionId = Id, ThisChannel = this });

            using (stream = thisClient.GetStream())
            {
                int position = 0;

                while(isOpen)
                {
                    if (clientDisconnected())
                    {
                        thisServer.OnClientDisconnected(new ClientStateArgs() { ConnectionId = Id, ThisChannel = this });
                        Close();
                    }
                    else
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
                    }
                }
            }
        }

        public void Send(byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }


        public void Close()
        {
            Dispose(false);
            isOpen = false;
            thisServer.ConnectedChannels.OpenChannels.TryRemove(Id, out Channel removedChannel);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }
                stream.Close();
                thisClient.Close();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool clientDisconnected()
        {
            return (thisClient.Client.Available == 0 && thisClient.Client.Poll(1, SelectMode.SelectRead));
        }
    }
}
