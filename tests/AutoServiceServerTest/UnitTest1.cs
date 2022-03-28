using System.Threading;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;

namespace AutoServiceServerTest;

public class UnitTest1
{
    private AutoServiceServerManager _serviceServerManager;
    
    [Fact]
    public void InitializeServer()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:l}{NewLine}{Exception}")
            .CreateLogger();

        var loggerFactory = new LoggerFactory()
            .AddSerilog(Log.Logger);

        _serviceServerManager = new AutoServiceServerManager("0.0.0.0", 2000, loggerFactory);
        _serviceServerManager.StartListening(cancellationTokenSource.Token);
    }
}