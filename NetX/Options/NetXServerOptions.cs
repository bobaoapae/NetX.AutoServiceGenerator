using System.Net;

namespace NetX.Options
{
    public class NetXServerOptions : NetXConnectionOptions
    {
        public INetXServerProcessor Processor { get; }

        public NetXServerOptions(
            INetXServerProcessor processor,
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
