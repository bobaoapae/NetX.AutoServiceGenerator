using System.Net;
using Microsoft.Extensions.Logging;

namespace NetX.Options
{
    public class NetXServerBuilder : NetXConnectionOptionsBuilder<INetXServer>, INetXServerOptionsProcessorBuilder, INetXServerOptionsBuilder
    {
        private ILoggerFactory _loggerFactory;
        private string _serverName;
        private INetXServerProcessor _processor;
        private bool _useProxy;
        private int _backlog;
        
        private NetXServerBuilder(ILoggerFactory loggerFactory, string serverName)
        {
            _loggerFactory = loggerFactory;
            _endpoint = new IPEndPoint(IPAddress.Any, 0);
            _useProxy = false;
            _backlog = 100;
        }

        public static INetXServerOptionsProcessorBuilder Create(ILoggerFactory loggerFactory = null, string serverName = null)
        {
            return new NetXServerBuilder(loggerFactory, serverName);
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

        public INetXServerOptionsBuilder Backlog(int backlog)
        {
            _backlog = backlog;
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
                _duplex,
                _duplexTimeout,
                _copyBuffer,
                _useProxy,
                _backlog);

            return new NetXServer(options, _loggerFactory, _serverName);
        }
    }
}
