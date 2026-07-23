# DotNetTemplate

一个 .NET 项目模板，生成类库 + CLI/GUI 示例 + 测试的完整解决方案。

## 安装

从 [GitHub Releases](https://github.com/zms9110750/DotNetTemplate/releases) 下载最新 `.nupkg` 并安装：

```bash
dotnet new install zms9110750.DotNetTemplate.*.nupkg
```

或直接使用模板源码目录：

```bash
git clone https://github.com/zms9110750/DotNetTemplate.git
dotnet new install DotNetTemplate/zmsTemplate
```

## 使用

```bash
dotnet new zms -n MyApp --TIsLib true --TIsCli true
```

参数说明（所有参数均为可选，默认 false）：

| 参数 | 说明 |
|------|------|
| `--TIsLib` | 类库 (src/) |
| `--TIsCli` | CLI 示例 |
| `--TIsGui` | GUI 示例 |
| `--TIsTest` | 测试项目 |
| `--TUsePolly` | Polly 弹性/重试 |
| `--TUseFusionCache` | FusionCache 缓存 |
| `--TUseDI` | DI 依赖注入容器 |
| `--TUseLog` | Serilog 日志 |
| `--TAuthorName` | 作者名 |

推荐组合：

```bash
# 全功能：类库 + CLI + 测试 + DI + 日志
dotnet new zms -n MyApp --TIsLib true --TIsCli true --TIsTest true --TUseDI true --TUseLog true

# 最小：纯 CLI 应用
dotnet new zms -n MyApp --TIsCli true
```

## 生成的项目

选择的开关决定项目结构：

- **有类库** → lib 在 `src/`，示例在 `samples/`
- **无类库** → 示例直接在 `src/`
- **测试** → 始终在 `test/`

所有项目的版本号、作者、仓库地址统一在 `Directory.Build.props` 中管理。`dotnet format` 在编译前自动执行。

## 打包

GitHub Actions 在推送 `v*` 标签时自动打包并发布到 Releases。手动打包：

```bash
dotnet pack DotNetTemplate.nuspec -o out
```

## 分支策略

```
master          ← 稳定分支
  └─ feature/*  ← 功能分支
```

PR 合并使用 Squash merge。
