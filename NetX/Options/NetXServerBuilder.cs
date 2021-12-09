using System.Net;

namespace NetX.Options
{
    public class NetXServerBuilder : NetXConnectionOptionsBuilder<INetXServer>, INetXServerOptionsProcessorBuilder, INetXServerOptionsBuilder
    {
        private INetXServerProcessor _processor;
        private bool _useProxy;
        
        private NetXServerBuilder()
        {
            _endpoint = new IPEndPoint(IPAddress.Any, 0);
        }

        public static INetXServerOptionsProcessorBuilder Create()
        {
            return new NetXServerBuilder();
        }

        public INetXServerOptionsBuilder Processor(INetXServerProcessor processorInstance)
        {
            _processor = processorInstance;
            return this;
        }

        public INetXServerOptionsBuilder Processor<T>() where T : INetXServerProcessor, new()
        {
            _processor = new T();
            return this;
        }

        public INetXServerOptionsBuilder UseProxy(bool useProxy)
        {
            _useProxy = useProxy;
            return this;
        }

        public override INetXServer Build()
        {
            var options = new NetXServerOptions(
                _processor, 
                _endpoint, 
                _noDelay, 
                _recvBufferSize, 
                _sendBufferSize, 
                _useCompletion,
                _useProxy);

            return new NetXServer(options);
        }
    }
}
