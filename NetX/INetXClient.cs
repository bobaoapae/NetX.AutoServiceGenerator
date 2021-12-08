using System.Threading;
using System.Threading.Tasks;

namespace NetX
{
    public interface INetXClient : INetXClientSession
    {
        Task ConnectAsync(CancellationToken cancellationToken = default);
    }
}
