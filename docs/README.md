# DotNetTemplate 项目文档

这份文档面向**以后接手这个仓库的 agent / 开发者**，目标是让你在不动手试错的前提下理解：
「这个东西想达成什么语义」以及「每一处为什么写成现在这样」。

## 一句话定位

`DotNetTemplate` 是一个 **`dotnet new` 模板包**（包名 `zms9110750.DotNetTemplate`，短名 `zms`），
用一条命令为**开源 .NET 项目**生成一整套可直接开工的仓库骨架：类库 + CLI/GUI 示例 + 测试 + CI/CD。

## 两层结构（先记住这个，否则后面会绕晕）

```
DotNetTemplate/                  ← 仓库根 = “模板包”这一层（用来打包和发布）
├── DotNetTemplate.nuspec        ← 把它打成一个 Template 类型的 nupkg
├── .github/workflows/ci.yml     ← 这一层自己的 CI（只打 nuspec，无 test/无矩阵发布）
├── out/                         ← 打包产物
└── zmsTemplate/                 ← 模板内容这一层（被 dotnet new 实例化成新项目）
    ├── .template.config/template.json
    ├── Directory.Build.props    ← 生成项目的“唯一配置中枢”
    ├── Global.cs                ← 所有项目共享的 global using + 基础设施装配
    ├── MyApp.slnx               ← 模板符号条件化（生成后按开关裁剪）
    ├── build/FormatOnce.proj    ← 让 dotnet format 在整次构建里只启动一次
    ├── src/  samples/  test/
    └── .github/workflows/ci.yml ← 生成项目自己的 CI（build/test/发布/预览包）
```

**同一个 `ci.yml` 文件名在两层各有一份，语义不同**，文档中分别称为「模板仓库 CI」和「模板内 CI」。
详见 [05-GitHub工作流](05-GitHub工作流.md)。

## 文档索引

| 文档 | 内容 |
|---|---|
| [01-语义与设计意图](01-语义与设计意图.md) | 这个项目想达成什么、设计目标、明确的边界（不做什么） |
| [02-模板机制](02-模板机制.md) | `template.json` 逐项解释、符号替换、条件语法、裁剪规则、打包 |
| [03-构建体系](03-构建体系.md) | `Directory.Build.props` 逐块解释、`Global.cs`、`.editorconfig`、format 的去重 |
| [04-项目结构与示例](04-项目结构与示例.md) | `src/` vs `samples/` 双布局、CLI 命令树、GUI、测试的每个设计决定 |
| [05-GitHub工作流](05-GitHub工作流.md) | 两套 workflow 逐 job 解释、三种发版方式、每处“为什么这么绕” |
| [06-约定与已知问题](06-约定与已知问题.md) | 编码约定、已知限制、踩过的坑、尚未验证的项 |

## 按任务速查

| 我遇到的情况 | 去看 |
|---|---|
| 想知道模板参数怎么影响生成结果 | [02](02-模板机制.md) 的「裁剪规则」 |
| 想改版本号 / 作者 / 仓库地址 | [03](03-构建体系.md) → `Directory.Build.props` 的“包元数据”块 |
| 想改条件包引用（Polly / FusionCache / DI / Log） | [03](03-构建体系.md) 的「条件包引用矩阵」 |
| 构建时为什么突然开始格式化、为什么只跑一次 | [03](03-构建体系.md) 的「FormatCode」 |
| CI 发版逻辑看不懂 / 要去掉草稿 | [05](05-GitHub工作流.md) 的「发版三方式」 |
| 生成的 `.cs` 文件被工具读成乱码 | [06](06-约定与已知问题.md) 的「文件编码」 |
| `dotnet format` 好像没生效但构建是绿的 | [06](06-约定与已知问题.md) 的「SDK 版本坑」 |

## 事实与推断的标注约定

本文档中：

- 未标注的陈述 = 已在本机实测或可从仓库文件直接读出；
- 标注 **（推断）** = 未经实测的推论，改动前请自行验证；
- 标注 **（未验证）** = 明确知道没验证，见 [06](06-约定与已知问题.md) 末尾清单。

## 实测环境

本文档中的实测数据来自：Windows，.NET SDK `10.0.303`（另有 `11.0.100-rc.1` 作为反例），
`dotnet format` 单次约 8–13 秒，详见 [03](03-构建体系.md) 与 [06](06-约定与已知问题.md)。
