using System.Threading.Tasks;

namespace NetX.AutoServiceGenerator.E2ETests.Definitions;

public interface INotificationService
{
    Task<bool> Notify(string message);
}
