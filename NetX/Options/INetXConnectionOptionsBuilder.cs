using System.Net;

namespace NetX.Options
{
    public interface INetXConnectionOptionsBuilder<T>
    {
        INetXConnectionOptionsBuilder<T> EndPoint(IPEndPoint endPoint);
        INetXConnectionOptionsBuilder<T> EndPoint(string address, ushort port);
        INetXConnectionOptionsBuilder<T> NoDelay(bool noDelay);
        INetXConnectionOptionsBuilder<T> Duplex(bool duplex);
        INetXConnectionOptionsBuilder<T> ReceiveBufferSize(int size);
        INetXConnectionOptionsBuilder<T> SendBufferSize(int size);
        T Build();
    }
}
