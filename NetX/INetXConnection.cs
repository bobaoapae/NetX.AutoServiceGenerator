using System;
using System.Threading.Tasks;

namespace NetX
{
    public interface INetXConnection
    {
        ValueTask SendAsync(ArraySegment<byte> buffer);
        ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> buffer);
        ValueTask ReplyAsync(Guid messageId, ArraySegment<byte> buffer);
        void Disconnect();
    }
}
