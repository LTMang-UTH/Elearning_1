using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetCoreServer
{
    public class TcpClient : IDisposable
    {
        public TcpClient(IPAddress address, int port) : this(new IPEndPoint(address, port)) {}
        public TcpClient(string address, int port) : this(new IPEndPoint(IPAddress.Parse(address), port)) {}
        public TcpClient(DnsEndPoint endpoint) : this(endpoint as EndPoint, endpoint.Host, endpoint.Port) {}
        public TcpClient(IPEndPoint endpoint) : this(endpoint as EndPoint, endpoint.Address.ToString(), endpoint.Port) {}

        private TcpClient(EndPoint endpoint, string address, int port)
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
        public Socket Socket { get; private set; }

        public long BytesPending { get; private set; }
        public long BytesSending { get; private set; }
        public long BytesSent { get; private set; }
        public long BytesReceived { get; private set; }

        public bool OptionDualMode { get; set; }
        public bool OptionKeepAlive { get; set; }
        public int OptionTcpKeepAliveTime { get; set; } = -1;
        public int OptionTcpKeepAliveInterval { get; set; } = -1;
        public int OptionTcpKeepAliveRetryCount { get; set; } = -1;
        public bool OptionNoDelay { get; set; }
        public int OptionReceiveBufferLimit { get; set; } = 0;
        public int OptionReceiveBufferSize { get; set; } = 8192;
        public int OptionSendBufferLimit { get; set; } = 0;
        public int OptionSendBufferSize { get; set; } = 8192;

        private SocketAsyncEventArgs _connectEventArg;
        public bool IsConnecting { get; private set; }
        public bool IsConnected { get; private set; }

        protected virtual Socket CreateSocket()
        {
            return new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public virtual bool Connect()
        {
            if (IsConnected || IsConnecting)
                return false;

            _receiveBuffer = new Buffer();
            _sendBufferMain = new Buffer();
            _sendBufferFlush = new Buffer();

            _connectEventArg = new SocketAsyncEventArgs();
            _connectEventArg.RemoteEndPoint = Endpoint;
            _connectEventArg.Completed += OnAsyncCompleted;
            _receiveEventArg = new SocketAsyncEventArgs();
            _receiveEventArg.Completed += OnAsyncCompleted;
            _sendEventArg = new SocketAsyncEventArgs();
            _sendEventArg.Completed += OnAsyncCompleted;

            Socket = CreateSocket();
            IsSocketDisposed = false;

            if (Socket.AddressFamily == AddressFamily.InterNetworkV6)
                Socket.DualMode = OptionDualMode;

            OnConnecting();

            try
            {
                Socket.Connect(Endpoint);
            }
            catch (SocketException ex)
            {
                SendError(ex.SocketErrorCode);

                _connectEventArg.Completed -= OnAsyncCompleted;
                _receiveEventArg.Completed -= OnAsyncCompleted;
                _sendEventArg.Completed -= OnAsyncCompleted;

                OnDisconnecting();

                Socket.Close();
                Socket.Dispose();

                _connectEventArg.Dispose();
                _receiveEventArg.Dispose();
                _sendEventArg.Dispose();

                OnDisconnected();
                return false;
            }

            if (OptionKeepAlive)
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            if (OptionTcpKeepAliveTime >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, OptionTcpKeepAliveTime);
            if (OptionTcpKeepAliveInterval >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, OptionTcpKeepAliveInterval);
            if (OptionTcpKeepAliveRetryCount >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, OptionTcpKeepAliveRetryCount);
            if (OptionNoDelay)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            _receiveBuffer.Reserve(OptionReceiveBufferSize);
            _sendBufferMain.Reserve(OptionSendBufferSize);
            _sendBufferFlush.Reserve(OptionSendBufferSize);

            BytesPending = 0;
            BytesSending = 0;
            BytesSent = 0;
            BytesReceived = 0;

            IsConnected = true;

            OnConnected();

            if (_sendBufferMain.IsEmpty)
                OnEmpty();

            return true;
        }

        public virtual bool Disconnect()
        {
            if (!IsConnected && !IsConnecting)
                return false;

            if (IsConnecting)
                Socket.CancelConnectAsync(_connectEventArg);

            _connectEventArg.Completed -= OnAsyncCompleted;
            _receiveEventArg.Completed -= OnAsyncCompleted;
            _sendEventArg.Completed -= OnAsyncCompleted;

            OnDisconnecting();

            try
            {
                try
                {
                    Socket.Shutdown(SocketShutdown.Both);
                }
                catch {}

                Socket.Close();
                Socket.Dispose();

                _connectEventArg.Dispose();
                _receiveEventArg.Dispose();
                _sendEventArg.Dispose();

                IsSocketDisposed = true;
            }
            catch {}

            IsConnected = false;
            _receiving = false;
            _sending = false;

            ClearBuffers();
            OnDisconnected();

            return true;
        }

        public virtual bool Reconnect()
        {
            if (!Disconnect())
                return false;

            return Connect();
        }

        public virtual bool ConnectAsync()
        {
            if (IsConnected || IsConnecting)
                return false;

            _receiveBuffer = new Buffer();
            _sendBufferMain = new Buffer();
            _sendBufferFlush = new Buffer();

            _connectEventArg = new SocketAsyncEventArgs();
            _connectEventArg.RemoteEndPoint = Endpoint;
            _connectEventArg.Completed += OnAsyncCompleted;
            _receiveEventArg = new SocketAsyncEventArgs();
            _receiveEventArg.Completed += OnAsyncCompleted;
            _sendEventArg = new SocketAsyncEventArgs();
            _sendEventArg.Completed += OnAsyncCompleted;

            Socket = CreateSocket();
            IsSocketDisposed = false;

            if (Socket.AddressFamily == AddressFamily.InterNetworkV6)
                Socket.DualMode = OptionDualMode;

            IsConnecting = true;
            OnConnecting();

            if (!Socket.ConnectAsync(_connectEventArg))
                ProcessConnect(_connectEventArg);

            return true;
        }

        public virtual bool DisconnectAsync() => Disconnect();

        public virtual bool ReconnectAsync()
        {
            if (!DisconnectAsync())
                return false;

            while (IsConnected)
                Thread.Yield();

            return ConnectAsync();
        }

        private bool _receiving;
        private Buffer _receiveBuffer;
        private SocketAsyncEventArgs _receiveEventArg;

        private readonly object _sendLock = new object();
        private bool _sending;
        private Buffer _sendBufferMain;
        private Buffer _sendBufferFlush;
        private SocketAsyncEventArgs _sendEventArg;
        private long _sendBufferFlushOffset;

        public virtual long Send(byte[] buffer) => Send(buffer.AsSpan());
        public virtual long Send(byte[] buffer, long offset, long size) => Send(buffer.AsSpan((int)offset, (int)size));

        public virtual long Send(ReadOnlySpan<byte> buffer)
        {
            if (!IsConnected || buffer.IsEmpty)
                return 0;

            long sent = Socket.Send(buffer, SocketFlags.None, out SocketError ec);

            if (sent > 0)
            {
                BytesSent += sent;
                OnSent(sent, BytesPending + BytesSending);
            }

            if (ec != SocketError.Success)
            {
                SendError(ec);
                Disconnect();
            }

            return sent;
        }

        public virtual long Send(string text) => Send(Encoding.UTF8.GetBytes(text));
        public virtual long Send(ReadOnlySpan<char> text) => Send(Encoding.UTF8.GetBytes(text.ToArray()));

        public virtual bool SendAsync(ReadOnlySpan<byte> buffer)
        {
            if (!IsConnected)
                return false;

            if (buffer.IsEmpty)
                return true;

            lock (_sendLock)
            {
                if (((_sendBufferMain.Size + buffer.Length) > OptionSendBufferLimit) && (OptionSendBufferLimit > 0))
                {
                    SendError(SocketError.NoBufferSpaceAvailable);
                    return false;
                }

                _sendBufferMain.Append(buffer);
                BytesPending = _sendBufferMain.Size;

                if (_sending)
                    return true;
                else
                    _sending = true;

                TrySend();
            }

            return true;
        }

        public virtual bool SendAsync(string text) => SendAsync(Encoding.UTF8.GetBytes(text));

        public virtual long Receive(byte[] buffer, long offset, long size)
        {
            if (!IsConnected || size == 0)
                return 0;

            long received = Socket.Receive(buffer, (int)offset, (int)size, SocketFlags.None, out SocketError ec);

            if (received > 0)
            {
                BytesReceived += received;
                OnReceived(buffer, 0, received);
            }

            if (ec != SocketError.Success)
            {
                SendError(ec);
                Disconnect();
            }

            return received;
        }

        public virtual void ReceiveAsync()
        {
            TryReceive();
        }

        private void TryReceive()
        {
            if (_receiving || !IsConnected)
                return;

            _receiving = true;
            _receiveEventArg.SetBuffer(_receiveBuffer.Data, 0, (int)_receiveBuffer.Capacity);

            if (!Socket.ReceiveAsync(_receiveEventArg))
                ProcessReceive(_receiveEventArg);
        }

        private void TrySend()
        {
            if (!IsConnected)
                return;

            lock (_sendLock)
            {
                if (_sendBufferFlush.IsEmpty)
                {
                    _sendBufferFlush = Interlocked.Exchange(ref _sendBufferMain, _sendBufferFlush);
                    _sendBufferFlushOffset = 0;
                    BytesPending = 0;
                    BytesSending += _sendBufferFlush.Size;

                    if (_sendBufferFlush.IsEmpty)
                    {
                        _sending = false;
                        OnEmpty();
                        return;
                    }
                }
            }

            _sendEventArg.SetBuffer(_sendBufferFlush.Data, (int)_sendBufferFlushOffset,
                (int)(_sendBufferFlush.Size - _sendBufferFlushOffset));

            if (!Socket.SendAsync(_sendEventArg))
                ProcessSend(_sendEventArg);
        }

        private void ClearBuffers()
        {
            lock (_sendLock)
            {
                _sendBufferMain.Clear();
                _sendBufferFlush.Clear();
                _sendBufferFlushOffset = 0;
                BytesPending = 0;
                BytesSending = 0;
            }
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (IsSocketDisposed)
                return;

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    if (ProcessReceive(e))
                        TryReceive();
                    break;
                case SocketAsyncOperation.Send:
                    if (ProcessSend(e))
                        TrySend();
                    break;
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            IsConnecting = false;

            if (e.SocketError == SocketError.Success)
            {
                IsConnected = true;
                TryReceive();
                OnConnected();
            }
            else
            {
                SendError(e.SocketError);
                OnDisconnected();
            }
        }

        private bool ProcessReceive(SocketAsyncEventArgs e)
        {
            long size = e.BytesTransferred;

            if (size > 0)
            {
                BytesReceived += size;
                OnReceived(_receiveBuffer.Data, 0, size);
            }

            _receiving = false;

            if (e.SocketError == SocketError.Success && size > 0)
                return true;

            DisconnectAsync();
            return false;
        }

        private bool ProcessSend(SocketAsyncEventArgs e)
        {
            long size = e.BytesTransferred;

            if (size > 0)
            {
                BytesSending -= size;
                BytesSent += size;
                _sendBufferFlushOffset += size;

                if (_sendBufferFlushOffset == _sendBufferFlush.Size)
                    _sendBufferFlush.Clear();

                OnSent(size, BytesPending + BytesSending);
            }

            if (e.SocketError == SocketError.Success)
                return true;

            DisconnectAsync();
            return false;
        }

        protected virtual void OnConnecting() {}
        protected virtual void OnConnected() {}
        protected virtual void OnDisconnecting() {}
        protected virtual void OnDisconnected() {}
        protected virtual void OnReceived(byte[] buffer, long offset, long size) {}
        protected virtual void OnSent(long sent, long pending) {}
        protected virtual void OnEmpty() {}
        protected virtual void OnError(SocketError error) {}

        private void SendError(SocketError error)
        {
            if (error == SocketError.ConnectionAborted ||
                error == SocketError.ConnectionRefused ||
                error == SocketError.ConnectionReset ||
                error == SocketError.OperationAborted ||
                error == SocketError.Shutdown)
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
                    DisconnectAsync();
                }

                IsDisposed = true;
            }
        }
    }
}
