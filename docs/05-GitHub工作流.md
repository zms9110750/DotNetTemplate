# 05 · GitHub 工作流

## 两套 workflow 的分工

仓库里有**两份** `.github/workflows/ci.yml`，名字一样、语义不同：

| | 模板仓库 CI（根目录） | 模板内 CI（`zmsTemplate/`） |
|---|---|---|
| 服务于 | **DotNetTemplate 这个仓库自己** | **由模板生成出来的项目** |
| 打包对象 | `DotNetTemplate.nuspec`（模板包本身） | 项目里的类库项目（`dotnet pack`） |
| `test` job | 无 | 有 |
| RID / TFM 矩阵发布 | 无 | 有（自包含 6 RID + FDD 按 TFM） |
| 版本来源 | `.nuspec` 里的 `<version>` | `Directory.Build.props` 里的 `<Version>` |
| 预览包命名 | `nupkg-pr-<PR号>` | 同左，另加 sha 变体 `nupkg-pr-<PR号>-<sha7>` |
| 触发 | 完全相同 | 完全相同 |

**为什么模板内 CI 要多一堆发布矩阵、而模板仓库不需要**：模板包本身只是一个"文件树压缩包"，
按 RID 发布毫无意义；而生成出来的项目**可能有可执行程序**，才需要自包含/FDD 分发。
两套 CI 的 job 名（`build` / `release` / `pr-preview` / `repack`）保持同名，是为了让人在两份文件间对照阅读。

两者的 `release.yml` 则完全一致，只做 changelog 分类：

```yaml
changelog:
  categories:
    - title: 🚀 新功能   labels: ["enhancement"]
    - title: 🐛 修复     labels: ["bug"]
    - title: 📖 文档     labels: ["documentation"]
    - title: 🔧 维护     labels: ["*"]      # 兜底：其余全部
```

---

## 触发方式（两份相同）

```yaml
on:
  pull_request:  { branches: ["main","master"], types: ["opened","synchronize","reopened","closed"] }
  issue_comment: { types: ["created"] }
  push:          { tags: ["v*"] }
  workflow_dispatch: { inputs: { version: ... } }
```

- `pull_request` 带 `closed` 是为了**在 PR 刚合并的那一刻**拿到 `merged=true` 做里程碑发版。
- `issue_comment` 是给 `repack` job 用的（下面会讲）。
- 所有 job 用 `if: github.event_name != 'issue_comment'` 把"评论触发"从常规流程里排除。

---

## 模板仓库 CI 逐 job

### `build`

1. **Determine .NET version**：`"$($year - 2016).0.x"` —— 用当年年份推出 GA 大版本
   （2016 是 .NET Core 1.0 那年），配 `dotnet-quality: ga`。
   **为什么**：每年新 SDK 发布时无需手改工作流，代价是**不支持预览版 TFM**（README 已声明）。
2. **Resolve build version**：按触发方式解析版本号，四种来源：
   - 网页按钮填了 → 用它（去掉 `v` 前缀）
   - 网页按钮没填 → 从 `DotNetTemplate.nuspec` 里正则读 `<version>`
   - tag push → `github.ref_name` 去 `v` 前缀
   - PR 合并 → 读**里程碑**标题（必须是 `vX.Y.Z`）
   - 普通 PR 验证**不解析版本**（不发版）；发版类事件解析不到版本就 `throw` 直接失败。
     **为什么**：避免悄悄发一个 `1.0.0` 之类的垃圾版本。
3. **Compute release flag**：决定 `should_release`。
   **为什么 PR 合并时要校验里程碑格式**：在**源头拦截**——里程碑非法就不发版，后续 job 全部跳过，
   不做无用功。

### `release`（`if: needs.build.outputs.should_release == 'true'`）

1. **checkout 用 `ref: merge_commit_sha || github.sha`**、`fetch-depth: 0`。
   **为什么**：PR 合并发版时 tag 必须打在**真实的合并提交**上；不指定的话可能打在 GitHub 预生成的
   `refs/pull/N/merge` 临时提交上，产生"孤儿 tag"（提交很快消失，tag 指向不存在的对象）。
2. **Resolve release tag** —— 这段是全仓库最绕的一处：

   ```powershell
   # 里程碑 vX.Y.Z → 抢占唯一 tag vX.Y.Z-build.{N}
   $n = 0
   while ($n -lt 100) {
     $tag = "v${ver}-build.$n"
     git tag $tag
     git push origin $tag
     if ($LASTEXITCODE -eq 0) { break }   # push 成功 = 抢到了
     $n++
   }
   ```

   **为什么用 `git push` 循环**：同一个里程碑可能被多次合并（多个 run 并发），需要一个**原子裁决**。
   `git push` 本身就是 CAS 操作——同名 tag 只有一个 run 能推成功，输家 `+1` 重试。
   **为什么不会死循环**：`GITHUB_TOKEN` 推送的 tag **不会**触发 `push: tags` 的 CI（GitHub 防递归机制）。
3. **Skip if release exists**：用 `gh release view <tag>` 查是否已有 Release（含草稿），有就跳过。
   **为什么**：发布草稿这个动作会再次触发 CI，若无此检查就会无限建草稿或覆盖已发布的版本。
4. **Pack nuspec**：
   ```powershell
   $nuspec = $nuspec -replace '<version>[^<]+</version>', "<version>$env:VERSION</version>"
   [System.IO.File]::WriteAllText(...)   # UTF-8 无 BOM
   dotnet pack tmp.csproj -p:NuspecFile=$(Resolve-Path DotNetTemplate.nuspec) -p:Version=...
   ```
   **两处必须这么写的原因**：
   - `-p:Version` **不会覆盖** nuspec 里的 `<version>` 元素 → 打包前必须动态替换（这是实测确认的坑，代码注释也写了）。
   - 用 `[System.IO.File]::WriteAllText` + `UTF8Encoding($false)` 而不是 `Set-Content`：
     PowerShell 默认用系统 ANSI(GBK) 写文件，会破坏 UTF-8 内容（与 `AGENTS.md` 里的规则同源）。
   - 生成一个空的 `tmp.csproj` 只是为了拿到 `dotnet pack` 的入口，真正内容由 `NuspecFile` 指定
     （`IncludeBuildOutput=false` 表示不产出程序集）。
5. **`softprops/action-gh-release@v2`，`draft: true`**。
   **为什么是草稿**：发版不可逆。产物先以草稿形式生成，人工在网页上检查后点 Publish 才真正公开——
   这是最后一道闸。

### `pr-preview`

PR 打开/更新时打一个预览包（版本 `0.0.0-pr.<PR号>.<sha7>`），上传 artifact，
并在 PR 上评论出**可点击的下载链接**（`actions/github-script` 查本次 run 的 artifact 列表拼链接）。

- `if: github.event.action == 'opened' || 'reopened'` —— 只在打开/重开时评论，避免每次 push 都刷评论。
- 版本号里带 sha7，**同一 PR 的不同提交产出的包不会互相覆盖**。

### `repack`（PR 评论触发）

在 PR 里评论 `@github-actions[bot] pack <sha7>` 可以**重新打某个提交的包**。流程：

1. 正则提取 sha7；
2. **校验 sha7 确实是本 PR 的某次提交**（分页拉 `pulls.listCommits` 比对前缀），不是就回复报错；
3. checkout 该提交 → 打包 → 上传 artifact → 评论下载链接。

**为什么要校验 sha7**：这是唯一一处"由外部输入（评论内容）驱动 checkout"的地方。
不做校验的话，任何人都能让 CI 去构建任意提交，属于明显的安全隐患。

---

## 模板内 CI 逐 job（与上不同的部分）

### `build` 多了两件事

```yaml
- run: dotnet restore
- run: dotnet build --no-restore -c Release
```

**为什么 restore 与 build 分开**：先 restore 一次，后续所有 job 都能 `--no-restore` 复用，
减少重复的网络与解析开销；同时让"restore 失败"与"编译失败"在日志里分开呈现。

**Classify projects** 是模板内独有的：扫描 `*.slnx`（读 XML 拿项目列表，比递归猜更准），
把项目分成三类并通过 `GITHUB_OUTPUT` 传给后续 job：

| 输出 | 内容 |
|---|---|
| `exe_json` | 可执行项目（`OutputType` 为 `Exe`/`WinExe`）及其 TFM 列表 |
| `lib_json` | 其余项目（= 可打包的库） |
| `tfm_json` | 全部涉及的 TFM 去重排序 |

**两个关键细节**：

- **先按"是不是测试项目"排除**（匹配 `Microsoft.NET.Test.Sdk` / `xunit` / `NUnit` / `MSTest` /
  `TestingPlatformDotnetTestSupport`），**再**判断 `OutputType`。
  **为什么顺序重要**：xUnit v3 的测试项目就是 `Exe`（MTP），不先排除会被当成可发布的程序（见 [04](04-项目结构与示例.md)）。
- 用 `"name<<$delim"` 的多行输出语法（分隔符带 GUID）写入 `GITHUB_OUTPUT`，
  因为 JSON 里可能包含换行。

### `test`

独立 job，**不 `needs: build`** —— 与 `build` 并行跑，只做 `dotnet restore` + `dotnet test -c Release --no-restore`。

**为什么独立**：测试失败不应该阻塞打包产物的生成（产物对 PR 预览仍然有用）；
但 `release` job `needs: [build, test, publish-*, pack]`，所以**发版前测试必须通过**。

### `publish-self-contained`

```yaml
strategy:
  fail-fast: false
  matrix:
    rid: [win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64]
```

**为什么是这 6 个 RID**：覆盖三大平台的主流架构。
**没有 `linux-x86`**：.NET 6 起官方移除 32 位 Linux 支持（README 已声明）。

**`Get-Highest` 选 TFM**：自包含发布只挑**最高** TFM，并按 `net > netfx > netstandard` 排序，
且过滤掉 `net<3`（.NET Core 3.0 之前不支持自包含）。**为什么**：一个项目声明 `net6.0;net8.0;net10.0`
时，自包含包只应该给最新的那一份。

**为什么每个发布都包在 `try/catch` 里**：不兼容的组合（例如 .NET Framework 目标在 Linux 上编译失败）
应该**跳过**而不是中断整个发版。`fail-fast: false` 也是同一目的——一个 RID 失败不影响其他 RID。

### `publish-fdd`

```yaml
strategy:
  matrix:
    fw: ${{ fromJson(needs.build.outputs.tfm_json) }}
```

按 TFM 分组发布**框架依赖**包，同一 TFM 下多个 exe 的产物**合并进一个 zip**
（`$(($succeeded -join '+'))-$fw.zip`）。**为什么合并**：用户按框架选一个包即可，而不是下载一堆小包。

### `pack`

遍历 `lib_json` 逐个 `dotnet pack -p:Version=$PACK_VERSION`。
`if: ... && needs.build.outputs.version != ''` —— **为什么**：PR 合并但里程碑非法时版本为空，
不过滤就会产出一个 `1.0.0` 的垃圾包。

### `release`

`needs: [build, test, publish-self-contained, publish-fdd, pack]`，
下载所有 artifact → 收集 `*.zip` / `*.nupkg` → 建**草稿** Release。
tag 解析与"防重复"逻辑与模板仓库 CI **完全相同**（见上）。

### `pr-preview` / `repack`

结构与模板仓库版一致，差别在于：
- 打包对象是**库项目**（来自 `lib_json`），不是 nuspec；
- artifact 上传用 `if-no-files-found: warn`（模板仓库版是 `error`）。
  **为什么用 warn**：纯 exe 方案（没开 `TIsLib`）没有任何库可打包，此时 PR 预览不应报错，
  评论里会提示"无 artifact：纯 exe 方案无库可打包"。

> 注：两份 workflow 的 PR 评论模板里，标题的 emoji 在模板内 CI 中显示为 `??`
> ——那是文件编码/转义问题留下的痕迹，不影响功能。

---

## 三种发版方式（`zmsTemplate/README.md` 的「发版」章节）

| 方式 | 操作 | 版本来源 | 结果 tag |
|---|---|---|---|
| 一 · 推标签 | `git tag v0.1.0 && git push origin v0.1.0` | tag 名 | `v0.1.0` |
| 二 · 网页按钮 | Actions → CI → Run workflow（可填版本号） | 填的版本，否则读 props | `v<版本>` |
| 三 · PR 合并 + 里程碑 | 给 PR 挂 `vX.Y.Z` 里程碑后合并 | 里程碑标题 | `vX.Y.Z-build.{N}`，N 自动递增 |

三者的后续流程完全相同：编译 + 测试 → 打产物 → **草稿** Release。

**方式三的两个约束**（README 明确写出）：
- 里程碑必须是**纯** `vX.Y.Z`，不带 `-pre` / `+meta`；
- 里程碑缺失或格式不对 → **不发版，但也不阻止合并**。

**发布后的防重复行为**：某版本已正式发布后再触发同名版本不会重复发版（CI 自动跳过）。
想重发必须先手动删除旧 Release 和 tag —— 这是"防死循环"逻辑的另一面。

---

## 这些"绕"的地方值不值得

大部分复杂度来自两个真实约束，理解它们就能接受这些写法：

1. **发版不可逆** → 草稿 Release + "已存在则跳过" + 里程碑格式源头拦截。
2. **CI 会被自己触发的动作再次触发** → tag 用 CAS 抢占 + `skip if exists` + `gh` 后显式 `exit 0`
   （`gh` 失败会残留 `$LASTEXITCODE=1`，不显式成功退出会让步骤被误判为失败）。

如果以后要简化，**优先考虑去掉"PR 合并 + 里程碑"这条发版路径**（方式三），
它是 tag 抢占逻辑存在的主要原因；只保留 tag push 与网页按钮，`release` job 会简单一大截。
