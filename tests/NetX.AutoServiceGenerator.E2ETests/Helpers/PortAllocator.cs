using System.Threading;

namespace NetX.AutoServiceGenerator.E2ETests.Helpers;

public static class PortAllocator
{
    private static int _currentPort = 19000;

    public static ushort GetNextPort()
    {
        return (ushort)Interlocked.Increment(ref _currentPort);
    }
}
