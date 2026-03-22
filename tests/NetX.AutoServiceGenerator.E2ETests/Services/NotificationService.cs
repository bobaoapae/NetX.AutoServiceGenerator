using System.Collections.Concurrent;
using System.Threading.Tasks;
using NetX.AutoServiceGenerator.E2ETests.Definitions;

namespace NetX.AutoServiceGenerator.E2ETests;

public partial class NotificationService : INotificationService
{
    public static ConcurrentBag<string> ReceivedNotifications { get; } = new();

    public Task<bool> Notify(string message)
    {
        ReceivedNotifications.Add(message);
        return Task.FromResult(true);
    }
}
