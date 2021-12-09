using System.Net;

namespace NetX.Options
{
    public class NetXClientBuilder : NetXConnectionOptionsBuilder<INetXClient>, INetXClientOptionsProcessorBuilder, INetXClientOptionsBuilder
    {
        private INetXClientProcessor _processor;

        private NetXClientBuilder()
        {
            _endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
        }

        public static INetXClientOptionsProcessorBuilder Create()
        {
            return new NetXClientBuilder();
        }

        public INetXClientOptionsBuilder Processor(INetXClientProcessor processorInstance)
        {
            _processor = processorInstance;
            return this;
        }

        public INetXClientOptionsBuilder Processor<T>() where T : INetXClientProcessor, new()
        {
            _processor = new T();
            return this;
        }

        public override INetXClient Build()
        {
            var options = new NetXClientOptions(
                _processor,
                _endpoint,
                _noDelay,
                _recvBufferSize,
                _sendBufferSize,
                _duplex,
                _duplexTimeout,
                _copyBuffer);

            return new NetXClient(options);
        }
    }
}
