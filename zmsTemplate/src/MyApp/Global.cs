// Global usings — MyApp
#if (UsePolly)
global using Polly;
global using Polly.Retry;
#endif
#if (UseFusionCache)
global using ZiggyCreatures.Caching.Fusion;
#endif
#if (UseDI)
global using Microsoft.Extensions.DependencyInjection;
#endif
#if (UseLog)
global using Serilog;
#endif

#if (IsTest)
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyApp.Test")]
#endif
