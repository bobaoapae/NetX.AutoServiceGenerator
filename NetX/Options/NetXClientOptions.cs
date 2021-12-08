using System.Net;

namespace NetX.Options
{
    internal class NetXClientOptions : NetXConnectionOptions
    {
        public INetXClientProcessor Processor { get; }

        public NetXClientOptions(
            INetXClientProcessor processor,
            IPEndPoint endPoint, 
            bool noDelay, 
            int recvBufferSize, 
            int sendBufferSize, 
            bool useCompletion) : base(
                endPoint, 
                noDelay, 
                recvBufferSize, 
                sendBufferSize, 
                useCompletion)
        {
            Processor = processor;
        }
    }
}
