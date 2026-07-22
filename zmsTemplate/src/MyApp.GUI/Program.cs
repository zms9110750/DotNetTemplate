using Photino.Blazor;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddMasaBlazor();

#if (TUseFusionCache)
        appBuilder.Services.AddSqliteCache("cache.db")
            .AddFusionCache()
            .WithRegisteredDistributedCache()
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
#if (TUsePolly)
            .AsHybridCache()
#endif
            ;
#endif

#if (TUseLog)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/app.log")
            .CreateLogger();
        appBuilder.Services.AddSerilog();
#endif

        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();

        app.MainBlazorWindow.Window
            .SetTitle("MyApp")
            .SetUseOsDefaultSize(false)
            .SetSize(853, 480);

        app.Run();
    }
}
