using Serilog;
using Serilog.Events;

namespace UtilityService.Api.Extensions;

public static class SerilogHelper
{
    public static void ConfigureLogging(string serviceName, string? seqUrl)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console(outputTemplate: $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] {serviceName} | {{Message:lj}}{{NewLine}}{{Exception}}")
            .WriteTo.Seq(seqUrl ?? "http://localhost:5341")
            .CreateLogger();
    }
}
