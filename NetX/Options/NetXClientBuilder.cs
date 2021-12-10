using Microsoft.Extensions.Logging;
using System.Net;

namespace NetX.Options
{
    public class NetXClientBuilder : NetXConnectionOptionsBuilder<INetXClient>, INetXClientOptionsProcessorBuilder, INetXClientOptionsBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _clientName;
        private INetXClientProcessor _processor;

        private NetXClientBuilder(ILoggerFactory loggerFactory = null, string clientName = null)
        {
            _loggerFactory = loggerFactory;
            _clientName = clientName;
            _endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
        }

        public static INetXClientOptionsProcessorBuilder Create(ILoggerFactory loggerFactory = null, string clientName = null)
        {
            return new NetXClientBuilder(loggerFactory, clientName);
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

            return new NetXClient(options, _loggerFactory, _clientName);
        }
    }
}
