using System.Threading;

namespace NetX
{
    public interface INetXServer
    {
        void Listen(CancellationToken cancellationToken = default);
    }
}
