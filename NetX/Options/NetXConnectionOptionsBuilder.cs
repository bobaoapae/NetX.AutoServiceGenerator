using System.Net;

namespace NetX.Options
{
    public abstract class NetXConnectionOptionsBuilder<T> : INetXConnectionOptionsBuilder<T>
    {
        protected IPEndPoint _endpoint;
        protected bool _noDelay = false;
        protected int _recvBufferSize = 1024;
        protected int _sendBufferSize = 1024;
        protected bool _useCompletion = false;

        public INetXConnectionOptionsBuilder<T> EndPoint(IPEndPoint endPoint)
        {
            _endpoint = endPoint;
            return this;
        }

        public INetXConnectionOptionsBuilder<T> EndPoint(string address, ushort port)
            => EndPoint(new IPEndPoint(IPAddress.Parse(address), port));

        public INetXConnectionOptionsBuilder<T> NoDelay(bool noDelay)
        {
            _noDelay = noDelay;
            return this;
        }

        public INetXConnectionOptionsBuilder<T> Duplex(bool completion)
        {
            _useCompletion = completion;
            return this;
        }

        public INetXConnectionOptionsBuilder<T> ReceiveBufferSize(int size)
        {
            _recvBufferSize = size;
            return this;
        }

        public INetXConnectionOptionsBuilder<T> SendBufferSize(int size)
        {
            _sendBufferSize = size;
            return this;
        }

        public abstract T Build();
    }
}
