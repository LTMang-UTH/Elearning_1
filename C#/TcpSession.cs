using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetCoreServer
{
    public class TcpSession : IDisposable
    {
        public TcpSession(TcpServer server)
        {
            Id = Guid.NewGuid();
            Server = server;
            OptionReceiveBufferSize = server.OptionReceiveBufferSize;
            OptionSendBufferSize = server.OptionSendBufferSize;
        }

        public Guid Id { get; }
        public TcpServer Server { get; }
        public Socket Socket { get; private set; }

        public long BytesPending { get; private set; }
        public long BytesSending { get; private set; }
        public long BytesSent { get; private set; }
        public long BytesReceived { get; private set; }

        public int OptionReceiveBufferLimit { get; set; } = 0;
        public int OptionReceiveBufferSize { get; set; } = 8192;
        public int OptionSendBufferLimit { get; set; } = 0;
        public int OptionSendBufferSize { get; set; } = 8192;

        public bool IsConnected { get; private set; }

        internal void Connect(Socket socket)
        {
            Socket = socket;
            IsSocketDisposed = false;

            _receiveBuffer = new Buffer();
            _sendBufferMain = new Buffer();
            _sendBufferFlush = new Buffer();

            _receiveEventArg = new SocketAsyncEventArgs();
            _receiveEventArg.Completed += OnAsyncCompleted;
            _sendEventArg = new SocketAsyncEventArgs();
            _sendEventArg.Completed += OnAsyncCompleted;

            if (Server.OptionKeepAlive)
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            if (Server.OptionTcpKeepAliveTime >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, Server.OptionTcpKeepAliveTime);
            if (Server.OptionTcpKeepAliveInterval >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, Server.OptionTcpKeepAliveInterval);
            if (Server.OptionTcpKeepAliveRetryCount >= 0)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, Server.OptionTcpKeepAliveRetryCount);
            if (Server.OptionNoDelay)
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);

            _receiveBuffer.Reserve(OptionReceiveBufferSize);
            _sendBufferMain.Reserve(OptionSendBufferSize);
            _sendBufferFlush.Reserve(OptionSendBufferSize);

            BytesPending = 0;
            BytesSending = 0;
            BytesSent = 0;
            BytesReceived = 0;

            IsConnected = true;
            TryReceive();

            if (!IsSocketDisposed)
                OnEmpty();
        }

        public virtual bool Disconnect()
        {
            if (!IsConnected)
                return false;

            _receiveEventArg.Completed -= OnAsyncCompleted;
            _sendEventArg.Completed -= OnAsyncCompleted;

            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Socket.Dispose();

                _receiveEventArg.Dispose();
                _sendEventArg.Dispose();

                IsSocketDisposed = true;
            }
            catch { }

            IsConnected = false;
            _receiving = false;
            _sending = false;

            ClearBuffers();
            Server.UnregisterSession(Id);

            return true;
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

        public virtual bool SendAsync(ReadOnlySpan<byte> buffer)
        {
            if (!IsConnected)
                return false;

            if (buffer.IsEmpty)
                return true;

            lock (_sendLock)
            {
                if (((_sendBufferMain.Size + buffer.Length) > OptionSendBufferLimit) && (OptionSendBufferLimit > 0))
                    return false;

                _sendBufferMain.Append(buffer);
                BytesPending = _sendBufferMain.Size;

                if (_sending)
                    return true;

                _sending = true;
                TrySend();
            }

            return true;
        }

        private void TryReceive()
        {
            if (_receiving || !IsConnected)
                return;

            try
            {
                _receiving = true;
                _receiveEventArg.SetBuffer(_receiveBuffer.Data, 0, (int)_receiveBuffer.Capacity);

                if (!Socket.ReceiveAsync(_receiveEventArg))
                    ProcessReceive(_receiveEventArg);
            }
            catch { }
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
                        return;
                    }
                }
            }

            try
            {
                _sendEventArg.SetBuffer(_sendBufferFlush.Data,
                    (int)_sendBufferFlushOffset,
                    (int)(_sendBufferFlush.Size - _sendBufferFlushOffset));

                if (!Socket.SendAsync(_sendEventArg))
                    ProcessSend(_sendEventArg);
            }
            catch { }
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

        private bool ProcessReceive(SocketAsyncEventArgs e)
        {
            if (!IsConnected)
                return false;

            long size = e.BytesTransferred;

            if (size > 0)
            {
                BytesReceived += size;
                Interlocked.Add(ref Server._bytesReceived, size);
            }

            _receiving = false;

            if (e.SocketError == SocketError.Success)
            {
                if (size > 0)
                    return true;
                else
                    Disconnect();
            }
            else
            {
                Disconnect();
            }

            return false;
        }

        private bool ProcessSend(SocketAsyncEventArgs e)
        {
            if (!IsConnected)
                return false;

            long size = e.BytesTransferred;

            if (size > 0)
            {
                BytesSending -= size;
                BytesSent += size;
                Interlocked.Add(ref Server._bytesSent, size);

                _sendBufferFlushOffset += size;

                if (_sendBufferFlushOffset == _sendBufferFlush.Size)
                {
                    _sendBufferFlush.Clear();
                    _sendBufferFlushOffset = 0;
                }
            }

            return e.SocketError == SocketError.Success;
        }

        protected virtual void OnEmpty() {}

        public bool IsDisposed { get; private set; }
        public bool IsSocketDisposed { get; private set; } = true;

        public void Dispose()
        {
            Disconnect();
            IsDisposed = true;
        }
    }
}
