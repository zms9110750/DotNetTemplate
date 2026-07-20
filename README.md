# DotNetTemplate

一个 .NET 项目模板，生成类库 + CLI/GUI 示例 + 测试的完整解决方案。

## 快速开始

```bash
dotnet new install zmsTemplate/
dotnet new jm -n MyApp -I -Is
```

| 参数 | 说明 |
|------|------|
| `-I` | 类库 (src/) |
| `-Is` | CLI 示例 |
| `-p:I` | GUI 示例 |
| `-p:Is` | 测试项目 |
| `-U` | Polly |
| `-Us` | FusionCache |
| `-p:U` | DI |
| `-p:Us` | Serilog |
| `-A` | 作者名 |

## 生成的项目

选择的开关决定项目结构：

- **有类库** → lib 在 `src/`，示例在 `samples/`
- **无类库** → 示例直接在 `src/`
- **测试** → 始终在 `test/`

所有项目的版本号、作者、仓库地址统一在 `Directory.Build.props` 中管理。`dotnet format` 在编译前自动执行。

## 打包

```bash
nuget pack DotNetTemplate.nuspec -OutputDirectory out
```

生成 `.nupkg` 后可上传 NuGet 或 GitHub Releases 分发。

## 分支策略

```
master          ← 稳定分支
  └─ feature/*  ← 功能分支
```

PR 合并使用 Squash merge。
