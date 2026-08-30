namespace MyApp;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
//-:cnd:noEmit
#if USE_LOG
        // Serilog 最先初始化 + 全局异常兜底（写日志后继续报错，不静默吞掉）
        GlobalUsing.InitLogging();
        GlobalUsing.AttachExceptionHandlers();
#endif
//+:cnd:noEmit

        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddMasaBlazor();

//-:cnd:noEmit
#if USE_FUSIONCACHE
        GlobalUsing.ConfigureFusionCache(appBuilder.Services);
#endif

#if USE_FUSIONCACHE && USE_POLLY
        // key = 程序集名（AddHttpClient / AddResilienceHandler 同名）
        GlobalUsing.ConfigureHttpClient(appBuilder.Services);
#endif
//+:cnd:noEmit

//-:cnd:noEmit
#if USE_LOG
        appBuilder.Services.AddSerilog();
#endif
//+:cnd:noEmit

#if (TWithSamples)
        appBuilder.RootComponents.Add<App>("#app");
#else
        appBuilder.RootComponents.Add<EmptyApp>("#app");
#endif

        var app = appBuilder.Build();

        app.MainBlazorWindow.Window
            .SetTitle("MyApp")
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 750);

        app.Run();
    }
}
