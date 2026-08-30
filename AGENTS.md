# 项目规则（自动加载，必须遵守）

## 文件编码

**PowerShell 写文件默认用系统 ANSI（GBK），会破坏 UTF-8 内容。**
含中文的文件（.yml / .cs / .md / .json / .razor 等）**必须**用 edit_file / write_file 工具修改，禁止用 PowerShell 的 `>` / `Out-File` / `Set-Content` / here-string 重定向写入。

必须用 PowerShell 写文件时，显式指定 UTF-8 无 BOM：
```powershell
[System.IO.File]::WriteAllText($path, $content, (New-Object System.Text.UTF8Encoding($false)))
```

改完含中文的文件后，用 Node 验证编码未被破坏：
```
node -e "const fs=require('fs');console.log(fs.readFileSync('<file>','utf8').includes('<中文片段>'))"
```

**症状识别**：工具能 grep 到中文但 read_file / Node 读乱码 → 文件是 GBK，需转 UTF-8。
