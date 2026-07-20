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

#if (UseFusionCache)
        appBuilder.Services.AddSqliteCache("cache.db")
            .AddFusionCache()
            .WithRegisteredDistributedCache()
            .WithSerializer(new FusionCacheSystemTextJsonSerializer())
#if (UsePolly)
            .AsHybridCache()
#endif
            ;
#endif

#if (UseLog)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/app.log")
            .CreateLogger();
        appBuilder.Services.AddSerilog();
#endif

        appBuilder.RootComponents.Add<App>("app");

        var app = appBuilder.Build();

        var (w, h) = GetScreenSize();
        var winW = (int)(w * 2.0 / 3);
        var winH = (int)(h * 2.0 / 3);

        app.MainWindow
            .SetTitle("MyApp")
            .SetUseOsDefaultSize(false)
            .SetSize(winW, winH);

        app.Run();
    }

    static (int Width, int Height) GetScreenSize()
    {
#if WINDOWS
        var sw = GetSystemMetrics(SM_CXSCREEN);
        var sh = GetSystemMetrics(SM_CYSCREEN);
        return (sw, sh);
#else
        return (1920, 1080);
#endif
    }

#if WINDOWS
    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
#endif
}
