using System.Runtime.InteropServices;

namespace ServerClientSample
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public readonly struct SampleHeader
    {
        public int MessageSize { get; }

        public SampleHeader(int messageSize)
        {
            MessageSize = messageSize;
        }
    }
}
