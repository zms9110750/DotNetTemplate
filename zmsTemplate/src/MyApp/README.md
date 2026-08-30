# MyApp

类库项目，核心业务逻辑放在这里。

## 说明

- 生成 XML 文档文件（`GenerateDocumentationFile`），裸用 DLL 也能看到注释提示
- 版本号、作者、仓库地址统一在根目录 `Directory.Build.props` 中管理
- 全局基础设施（日志 / 缓存 / HttpClient）由根目录 `Global.cs` 的 `AppBootstrap` 装配

## 使用

```csharp
using MyApp;

var service = new MyService();
```

## 打包

```bash
dotnet pack src/MyApp/MyApp.csproj -c Release
```
