// Global usings
#if USE_POLLY
global using Polly;
global using Polly.Retry;
#endif
#if USE_FUSIONCACHE
global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
global using ZiggyCreatures.Caching.Fusion.MicrosoftHybridCache;
global using NeoSmart.Caching.Sqlite;
#endif
#if USE_DI
global using Microsoft.Extensions.DependencyInjection;
#endif
#if (USE_DI && USE_FUSIONCACHE && USE_POLLY)
global using Microsoft.Extensions.Caching.Hybrid;
global using Axion.Extensions.Caching.Hybrid.Serialization.Http;
global using Axion.Extensions.Http.Resilience;
#endif
#if USE_LOG
global using Serilog;
#endif

// Project-type usings
#if IS_CLI
global using System.CommandLine;
#endif
#if IS_GUI
global using Masa.Blazor;
#endif
#if IS_TEST
global using Xunit;
#endif
