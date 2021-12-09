using System;
using System.IO;
using System.Threading.Tasks;

namespace NetX
{
    public interface INetXConnection
    {
        ValueTask SendAsync(ArraySegment<byte> buffer);
        ValueTask SendAsync(Stream stream);

        ValueTask<ArraySegment<byte>> RequestAsync(ArraySegment<byte> buffer);
        ValueTask<ArraySegment<byte>> RequestAsync(Stream stream);

        ValueTask ReplyAsync(Guid messageId, ArraySegment<byte> buffer);
        ValueTask ReplyAsync(Guid messageId, Stream stream);

        void Disconnect();
    }
}
