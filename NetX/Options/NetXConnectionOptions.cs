using System.Net;

namespace NetX.Options
{
    public class NetXConnectionOptions
    {
        public IPEndPoint EndPoint { get; }
        public bool NoDelay { get; }
        public int RecvBufferSize { get; set; }
        public int SendBufferSize { get; set; }
        public bool Duplex { get; set; }

        public NetXConnectionOptions(
            IPEndPoint endPoint,
            bool noDelay,
            int recvBufferSize,
            int sendBufferSize,
            bool useCompletion)
        {
            EndPoint = endPoint;
            NoDelay = noDelay;
            RecvBufferSize = recvBufferSize;
            SendBufferSize = sendBufferSize;
            Duplex = useCompletion;
        }
    }
}
