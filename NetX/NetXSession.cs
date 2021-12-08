using System;
using NetX.Options;

namespace NetX
{
    public class NetXSession : NetXConnection, INetXSession
    {
        protected override int GetReceiveMessageSize(in ArraySegment<byte> buffer)
            => ((NetXServerOptions)_options).Processor.GetReceiveMessageSize(this, in buffer);

        protected override void ProcessReceivedBuffer(in ArraySegment<byte> buffer)
        {
            ((NetXServerOptions)_options).Processor.ProcessReceivedBuffer(this, in buffer);
            base.ProcessReceivedBuffer(buffer);
        }

        protected override void OnReceivedMessage(in NetXMessage message)
            => ((NetXServerOptions)_options).Processor.OnReceivedMessage(this, in message);

        protected override void ProcessSendBuffer(in ArraySegment<byte> buffer)
        {
            ((NetXServerOptions)_options).Processor.ProcessSendBuffer(this, in buffer);
            base.ProcessSendBuffer(buffer);
        }
    }
}
