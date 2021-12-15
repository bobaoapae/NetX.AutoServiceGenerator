using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetX.Options;

namespace NetX
{
    public sealed class NetXSession : NetXConnection, INetXSession
    {
        public Guid Id { get; }
        public IPAddress RemoteAddress { get; }
        public DateTime ConnectionTime { get; }

        public NetXSession(Socket socket, IPAddress remoteAddress, NetXServerOptions options, string serverName, ILogger logger) 
            : base(socket, options, serverName, logger, false)
        {
            Id = Guid.NewGuid();
            RemoteAddress = remoteAddress;
            ConnectionTime = DateTime.UtcNow;
        }

        protected override Task OnReceivedMessageAsync(NetXMessage message)
            => ((NetXServerOptions)_options).Processor.OnReceivedMessageAsync(this, message);

        protected override int GetReceiveMessageSize(in ArraySegment<byte> buffer)
            => ((NetXServerOptions)_options).Processor.GetReceiveMessageSize(this, in buffer);

        protected override void ProcessReceivedBuffer(in ArraySegment<byte> buffer)
        {
            ((NetXServerOptions)_options).Processor.ProcessReceivedBuffer(this, in buffer);
            base.ProcessReceivedBuffer(buffer);
        }

        protected override void ProcessSendBuffer(in ArraySegment<byte> buffer)
        {
            ((NetXServerOptions)_options).Processor.ProcessSendBuffer(this, in buffer);
            base.ProcessSendBuffer(buffer);
        }
    }
}
