using System.Net;

namespace NetX.Options
{
    public interface INetXConnectionOptionsBuilder<T>
    {
        INetXConnectionOptionsBuilder<T> EndPoint(IPEndPoint endPoint);
        INetXConnectionOptionsBuilder<T> EndPoint(string address, ushort port);
        INetXConnectionOptionsBuilder<T> NoDelay(bool noDelay);
        INetXConnectionOptionsBuilder<T> Duplex(bool duplex, int timeout = 5000);
        INetXConnectionOptionsBuilder<T> CopyBuffer(bool copyBuffer);
        INetXConnectionOptionsBuilder<T> ReceiveBufferSize(int size);
        INetXConnectionOptionsBuilder<T> SendBufferSize(int size);
        T Build();
    }
}
