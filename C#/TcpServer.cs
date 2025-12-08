using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetCoreServer
{
    public class TcpServer : IDisposable
    {
        public TcpServer(IPAddress address, int port) : this(new IPEndPoint(address, port)) {}
        public TcpServer(string address, int port) : this(new IPEndPoint(IPAddress.Parse(address), port)) {}
        public TcpServer(DnsEndPoint endpoint) : this(endpoint as EndPoint, endpoint.Host, endpoint.Port) {}
        public TcpServer(IPEndPoint endpoint) : this(endpoint as EndPoint, endpoint.Address.ToString(), endpoint.Port) {}

        private TcpServer(EndPoint endpoint, string address, int port)
        {
            Id = Guid.NewGuid();
            Address = address;
            Port = port;
            Endpoint = endpoint;
        }

        public Guid Id { get; }
        public string Address { get; }
        public int Port { get; }
        public EndPoint Endpoint { get; private set; }

        public long ConnectedSessions { get { return Sessions.Count; } }
        public long BytesPending { get { return _bytesPending; } }
        public long BytesSent { get { return _bytesSent; } }
        public long BytesReceived { get { return _bytesReceived; } }

        public int OptionAcceptorBacklog { get; set; } = 1024;
        public bool OptionDualMode { get; set; }
        public bool OptionKeepAlive { get; set; }
        public int OptionTcpKeepAliveTime { get; set; } = -1;
        public int OptionTcpKeepAliveInterval { get; set; } = -1;
        public int OptionTcpKeepAliveRetryCount { get; set; } = -1;
        public bool OptionNoDelay { get; set; }
        public bool OptionReuseAddress { get; set; }
        public bool OptionExclusiveAddressUse { get; set; }
        public int OptionReceiveBufferSize { get; set; } = 8192;
        public int OptionSendBufferSize { get; set; } = 8192;

        private Socket _acceptorSocket;
        private SocketAsyncEventArgs _acceptorEventArg;

        internal long _bytesPending;
        internal long _bytesSent;
        internal long _bytesReceived;

        public bool IsStarted { get; private set; }
        public bool IsAccepting { get; private set; }

        protected virtual Socket CreateSocket()
        {
            return new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public virtual bool Start()
        {
            Debug.Assert(!IsStarted, "TCP server is already started!");
            if (IsStarted)
                return false;

            _acceptorEventArg = new SocketAsyncEventArgs();
            _acceptorEventArg.Completed += OnAsyncCompleted;

            _acceptorSocket = CreateSocket();
            IsSocketDisposed = false;

            _acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, OptionReuseAddress);
            _acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, OptionExclusiveAddressUse);

            if (_acceptorSocket.AddressFamily == AddressFamily.InterNetworkV6)
                _acceptorSocket.DualMode = OptionDualMode;

            _acceptorSocket.Bind(Endpoint);
            Endpoint = _acceptorSocket.LocalEndPoint;

            OnStarting();

            _acceptorSocket.Listen(OptionAcceptorBacklog);

            _bytesPending = 0;
            _bytesSent = 0;
            _bytesReceived = 0;

            IsStarted = true;

            OnStarted();

            IsAccepting = true;
            StartAccept(_acceptorEventArg);

            return true;
        }

        public virtual bool Stop()
        {
            Debug.Assert(IsStarted, "TCP server is not started!");
            if (!IsStarted)
                return false;

            IsAccepting = false;

            _acceptorEventArg.Completed -= OnAsyncCompleted;

            OnStopping();

            try
            {
                _acceptorSocket.Close();
                _acceptorSocket.Dispose();
                _acceptorEventArg.Dispose();
                IsSocketDisposed = true;
            }
            catch (ObjectDisposedException) {}

            DisconnectAll();

            IsStarted = false;

            OnStopped();

            return true;
        }

        public virtual bool Restart()
        {
            if (!Stop())
                return false;

            while (IsStarted)
                Thread.Yield();

            return Start();
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;

            if (!_acceptorSocket.AcceptAsync(e))
                ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var session = CreateSession();
                RegisterSession(session);
                session.Connect(e.AcceptSocket);
            }
            else
                SendError(e.SocketError);

            if (IsAccepting)
                StartAccept(e);
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (IsSocketDisposed)
                return;

            ProcessAccept(e);
        }

        protected virtual TcpSession CreateSession() { return new TcpSession(this); }

        protected readonly ConcurrentDictionary<Guid, TcpSession> Sessions = new ConcurrentDictionary<Guid, TcpSession>();

        public virtual bool DisconnectAll()
        {
            if (!IsStarted)
                return false;

            foreach (var session in Sessions.Values)
                session.Disconnect();

            return true;
        }

        public TcpSession FindSession(Guid id)
        {
            return Sessions.TryGetValue(id, out TcpSession result) ? result : null;
        }

        internal void RegisterSession(TcpSession session)
        {
            Sessions.TryAdd(session.Id, session);
        }

        internal void UnregisterSession(Guid id)
        {
            Sessions.TryRemove(id, out TcpSession _);
        }

        public virtual bool Multicast(byte[] buffer) => Multicast(buffer.AsSpan());
        public virtual bool Multicast(byte[] buffer, long offset, long size) => Multicast(buffer.AsSpan((int)offset, (int)size));

        public virtual bool Multicast(ReadOnlySpan<byte> buffer)
        {
            if (!IsStarted)
                return false;

            if (buffer.IsEmpty)
                return true;

            foreach (var session in Sessions.Values)
                session.SendAsync(buffer);

            return true;
        }

        public virtual bool Multicast(string text) => Multicast(Encoding.UTF8.GetBytes(text));
        public virtual bool Multicast(ReadOnlySpan<char> text) => Multicast(Encoding.UTF8.GetBytes(text.ToArray()));

        protected virtual void OnStarting() {}
        protected virtual void OnStarted() {}
        protected virtual void OnStopping() {}
        protected virtual void OnStopped() {}

        protected virtual void OnConnecting(TcpSession session) {}
        protected virtual void OnConnected(TcpSession session) {}
        protected virtual void OnDisconnecting(TcpSession session) {}
        protected virtual void OnDisconnected(TcpSession session) {}

        protected virtual void OnError(SocketError error) {}

        internal void OnConnectingInternal(TcpSession session) { OnConnecting(session); }
        internal void OnConnectedInternal(TcpSession session) { OnConnected(session); }
        internal void OnDisconnectingInternal(TcpSession session) { OnDisconnecting(session); }
        internal void OnDisconnectedInternal(TcpSession session) { OnDisconnected(session); }

        private void SendError(SocketError error)
        {
            if ((error == SocketError.ConnectionAborted) ||
                (error == SocketError.ConnectionRefused) ||
                (error == SocketError.ConnectionReset) ||
                (error == SocketError.OperationAborted) ||
                (error == SocketError.Shutdown))
                return;

            OnError(error);
        }

        public bool IsDisposed { get; private set; }
        public bool IsSocketDisposed { get; private set; } = true;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposingManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposingManagedResources)
                {
                    Stop();
                }

                IsDisposed = true;
            }
        }
    }
}
