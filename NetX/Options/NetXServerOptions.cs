using System.Net;

namespace NetX.Options
{
    public class NetXServerOptions : NetXConnectionOptions
    {
        public INetXServerProcessor Processor { get; }
        public bool UseProxy { get; }

        public NetXServerOptions(
            INetXServerProcessor processor,
            IPEndPoint endPoint, 
            bool noDelay, 
            int recvBufferSize, 
            int sendBufferSize, 
            bool useCompletion,
            bool useProxy) : base(
                endPoint, 
                noDelay, 
                recvBufferSize, 
                sendBufferSize, 
                useCompletion)
        {
            Processor = processor;
            UseProxy = useProxy;
        }
    }
}
