using System;

namespace NetX
{
    public readonly struct NetXMessage
    {
        public Guid Id { get; }
        public ArraySegment<byte> Buffer { get; }

        public NetXMessage(Guid id, ArraySegment<byte> message)
        {
            Id = id;
            Buffer = message;
        }
    }
}
