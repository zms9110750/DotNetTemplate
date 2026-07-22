# {{AuthorName}}.MyApp

这是一个由 **zmsTemplate** 生成的开源项目。

---

## 特性

### 编译自动格式化

项目根目录有 `.editorconfig`，每次 `dotnet build` 前自动执行 `dotnet format`。代码风格统一，无需手动整理。

### GitHub 工作流

`.github/workflows/ci.yml` 包含了完整的 CI/CD：

- **PR 到 main/master** — 自动 `dotnet restore` → `build` → `test`，测试通过才能合并
- **推送 `v*` 标签** — 自动打包发版

### 集中配置

所有项目的版本号、作者、仓库地址统一写在 `Directory.Build.props` 中。修改版本只需改这一个文件。

### 解决方案结构

解决方案已按文件夹组织：

//#if (TIsLib)
- `/src/` — 类库
  //#if (TIsCli)
- `/samples/` — CLI 示例
  //#endif
  //#if (TIsGui)
- `/samples/` — GUI 示例
  //#endif
//#else
  //#if (TIsCli)
- `/src/` — CLI 应用
  //#endif
  //#if (TIsGui)
- `/src/` — GUI 应用
  //#endif
//#endif
//#if (TIsTest)
- `/test/` — 测试项目
//#endif

---

## 项目说明

//#if (TIsCli)
### CLI

基于 `System.CommandLine` 的命令行应用。入口在 `Tree/CliRoot.cs`。

**文件布局：**

```
Tree/                     ← 镜像命令树层级
├── CliRoot.cs            ← 根命令，注册所有子命令
├── Rand/
│   └── RandCommand.cs    ← rand 子命令：生成随机数
├── Hello/
│   └── HelloCommand.cs   ← hello 子命令：输出问候语
```

- 每个命令的 `SetAction` 提取为独立的成员方法
- 命令树深一层，`Tree/` 里就深一层文件夹
- `Options/` 放 `enum`，`Shared/` 放复用 `Option` 实例

**命令说明：**

| 命令 | 参数 | 说明 |
|------|------|------|
| `（无子命令）` | `[items...]`（可选） | 逐行输出所有参数值 |
| `rand` | `--min`（默认 1）、`--max`（默认 100）、`--count`（默认 1） | 生成指定数量的随机整数 |
| `hello` | `<name>`（必填） | 输出 `hello <name>` |
//#endif

//#if (TIsGui)
### GUI

基于 `Photino.Blazor` + `Masa.Blazor` 的跨平台桌面应用。无需 WPF/WinForms 依赖，Windows、Linux、macOS 均可运行。

窗口大小默认 1280×720。
//#endif

//#if (TIsLib)
### 类库

核心逻辑放在 `src/MyApp/`，//#if (TIsCli && TIsGui)
CLI 和 GUI 通过项目引用调用。
//#elseif (TIsCli)
CLI 通过项目引用调用。
//#elseif (TIsGui)
GUI 通过项目引用调用。
//#endif
生成 XML 文档文件，裸用 DLL 也能看到注释提示。
//#endif

---

//#if (TUseDI || TUseFusionCache || TUsePolly || TUseLog)
## 包使用指南

//#if (TUseDI)
### DI 模式

依赖注入（DI）容器通过 `ServiceCollection` 构建，在应用入口处完成注册、构建、解析：

```csharp
var services = new ServiceCollection();

// 注册服务
services.AddSingleton<IMyService, MyService>();

// 构建容器
var provider = services.BuildServiceProvider();

// 解析使用
var myService = provider.GetRequiredService<IMyService>();
```

以下各节为按特性注册的扩展方法调用，统一在 `BuildServiceProvider()` 之前执行。

//#if (TUseFusionCache)
#### FusionCache

  //#if (TUsePolly)

```csharp
services.AddSqliteCache("cache.db")
    .AddFusionCache()
    .WithRegisteredDistributedCache()
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .AsHybridCache();  // 桥接到 HybridCache，供下方 Polly 缓存使用
```

`.AsHybridCache()` 将 FusionCache 桥接到 `Microsoft.Extensions.Caching.Hybrid`，供下方 Polly 缓存策略使用。

  //#else

```csharp
services.AddSqliteCache("cache.db")
    .AddFusionCache()
    .WithRegisteredDistributedCache()
    .WithSerializer(new FusionCacheSystemTextJsonSerializer());
```

  //#endif
//#endif

//#if (TUsePolly)
#### Polly

  //#if (TUseFusionCache)
##### Polly + FusionCache

使用 `Axion.Extensions.Http.Resilience.Caching.Hybrid`（`HybridCache` 来自上方 `.AsHybridCache()`）：

```csharp
services.AddHybridCache()
    .AddSerializer(HttpResponseMessageHybridCacheSerializer.Instance);

services.AddHttpClient("MyClient")
    .AddResilienceHandler("MyHandler", (pipeline, context) =>
    {
        pipeline.AddCaching(new HttpCachingStrategyOptions
        {
            HybridCache = context.ServiceProvider.GetRequiredService<HybridCache>()
        });
    });
```

已自动包含 Polly.Core，无需单独引用。

  //#else
使用 `Microsoft.Extensions.Http.Resilience` 自定义管道：

```csharp
services.AddHttpClient("MyClient")
    .AddResilienceHandler("MyPipeline", static builder =>
    {
        // 429 重试：最多 3 次，指数退避
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = static args =>
                ValueTask.FromResult(args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests)
        });

        // 请求限流：每秒最多 3 个
        builder.AddRateLimiter(new HttpRateLimiterStrategyOptions
        {
            DefaultRateLimiterOptions = new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromSeconds(1),
                SegmentsPerWindow = 1
            }
        });

        // 总超时：30 秒
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });
    });
```

  //#endif
//#endif

//#if (TUseLog)
#### Serilog

```csharp
builder.Services.AddSerilog((provider, config) =>
{
    config
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(provider)
        .WriteTo.Console()
        .WriteTo.File("logs/app.log");
});
```

Serilog 的配置从 `appsettings.json` 的 `Serilog` 节读取（不是 `Logging.LogLevel`），`Serilog.Extensions.Logging` 桥接包将 Serilog 接入 `Microsoft.Extensions.Logging` 抽象。
//#endif

//#if (TUseDI && TUseLog)
#### Soenneker.HttpClients.LoggingHandler

启用后自动为 HTTP 请求添加日志输出，便于调试。
//#endif

//#else
//#if (TUseFusionCache)
#### FusionCache

```csharp
SQLitePCL.Batteries_V2.Init();

var cache = new FusionCache(
    Microsoft.Extensions.Options.Options.Create(new FusionCacheOptions())
);

var l2Cache = new SqliteCache(new SqliteCacheOptions { CachePath = "cache.db" });
var serializer = new FusionCacheSystemTextJsonSerializer();
cache.SetupDistributedCache(l2Cache, serializer);
```

  //#if (TUsePolly)
FusionCache 默认不提供 HybridCache 适配器，需用 `FusionHybridCache` 手动包装：

```csharp
var fusionCache = new FusionCache(new FusionCacheOptions());
var hybridCache = new FusionHybridCache(fusionCache);
```

  //#endif
//#endif

//#if (TUsePolly)
#### Polly

  //#if (TUseFusionCache)
##### Polly + FusionCache

使用 `Axion.Extensions.Polly.Caching.Hybrid`，将上面的 `hybridCache` 传入：

```csharp
var fusionCache = new FusionCache(new FusionCacheOptions());
var hybridCache = new FusionHybridCache(fusionCache);

var pipeline = new ResiliencePipelineBuilder()
    .AddCaching(new CachingStrategyOptions
    {
        HybridCache = hybridCache
    })
    .Build();
```

  //#else
```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1)
    })
    .Build();

pipeline.Execute(() => Console.WriteLine("OK"));
```

  //#endif
//#endif

//#if (TUseLog)
#### Serilog

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log")
    .CreateLogger();

Log.Information("App started");
```

Serilog 的配置从 `appsettings.json` 的 `Serilog` 节读取（不是 `Logging.LogLevel`）。
//#endif

//#endif

//#endif

---

## 开发流程

### 分支策略

```
main          ← 稳定分支，PR 合并目标
  └─ feature/xxx  ← 功能分支，from main 分出
```

所有改动在功能分支上进行，完成后提交 Pull Request 到 `main`。

- 分支命名：`feature/简短描述` 或 `fix/简短描述`
- PR 标题：清晰说明改动内容
- 合并方式：**Squash merge**（将分支上所有提交压缩为一个提交）

### 发版

推送 `v*` 标签（如 `v0.1.0`）时，GitHub Actions 自动：

1. 编译 + 测试
2. 打 nupkg
3. 创建 GitHub Release
4. Release Notes 根据 PR 标签自动分类生成

标签名即版本号，与 `Directory.Build.props` 中的 `<Version>` 保持一致。

**推送标签：** 在项目目录执行以下命令，后续 `git push` 会自动携带标签：

```bash
git config push.followTags true
git push
```

首次推送标签可用 `git push --tags`。

---

## 分发说明

GitHub Actions CI 在 `v*` 标签推送时自动构建并发布以下产物：

| 类型 | 说明 |
|------|------|
| **自包含 zip** | 6 个 RID（win-x64/arm64, linux-x64/arm64, osx-x64/arm64），基于最高 TFM 发布 |
| **FDD zip** | 按 TFM 分组，同一 TFM 的多个 exe 合并到同一 zip |
| **NuGet** | 类库项目的 `.nupkg` 包 |

### 限制

- **.NET Framework**（net472/net48 等）：CI 运行在 Linux runner 上，不支持发布基于 Framework 的项目。如果你的项目必须发布 .NET Framework 版本，请在 Windows runner 上自行构建
- **自包含发布的目标框架**：CI 的 `Get-Highest` 函数自动选择项目中的最高 TFM（如 net6.0;net8.0;net9.0 选 net9.0）。如需发布特定 TFM 的自包含包，请调整 `TargetFrameworks` 或手动构建
- **Linux x86-32**：.NET 6 起已移除对 32 位 Linux 的官方支持，本 CI 不提供 `linux-x86` RID
