# 桌面管理器

桌面管理器是一个 Windows 桌面图标整理与桌面管理工具。它可以把桌面图标按左侧、右侧、顶部、底部、中间紧凑网格、自定义图案或桌面分区进行摆放，并提供方案、场景、快照恢复、文件收纳建议和更新提醒。

当前开源版本：`1.1.5`

> 普通“应用布局”只修改 Windows 桌面图标的显示坐标，不删除、不移动、不重命名、不修改真实文件。真实文件移动只存在于独立“文件收纳”页面，并且需要逐项勾选和二次确认。

## 下载安装

最新安装包在 GitHub Releases：

[https://github.com/SorrowGod/desktop-icon-manager/releases/latest](https://github.com/SorrowGod/desktop-icon-manager/releases/latest)

推荐普通用户下载：

- `DesktopIconManagerSetup-版本号-x64.exe`：安装版，带中文安装向导、桌面快捷方式和开始菜单入口。
- `DesktopIconManagerPortable-版本号-x64.zip`：便携版，解压后直接运行。

当前更新清单地址：

```text
https://sorrowgod.github.io/desktop-icon-manager/update.json
```

应用内只会提示更新和打开下载页，不会静默下载或自动安装。

## 主要功能

- 整理布局：左边、右边、上面、下面、中间一团、自定义图案、桌面分区。
- 排序规则：类型分组、名称 A-Z、保持当前顺序、最近修改时间、文件大小、扩展名、使用频率、手动优先级。
- 自定义图案：圆形、半圆、心形、星形、波浪线、斜线、方阵、环形、V 字形、X 字形、文字轮廓、图片蒙版、手动点位。
- 桌面分区：常用软件、工作文件、媒体与压缩包、待处理等区域规则；分区只是坐标规则，不创建真实文件夹。
- 图标管理：搜索、筛选、排除、排除系统图标、标签和规则。
- 方案中心：保存多套布局、排序、分区、图案、排除项、快照保留数和自启动设置。
- 场景模式：工作、学习、游戏、演示、极简等内置场景可编辑保存，自定义场景可新增删除。
- 快照恢复：每次应用前自动保存布局快照，支持恢复和撤销上次整理。
- 文件收纳：独立页面，可把桌面文件移动到桌面分类文件夹，并支持撤销；普通整理不会触发收纳。
- 诊断支持：导出本地诊断包，默认脱敏路径，不自动上传任何日志。
- 更新提醒：读取公开 JSON 清单，只提示下载，不自动安装。

## 安全边界

普通整理功能只通过 Windows 桌面 `SysListView32` 修改图标显示坐标。

它不会：

- 删除文件
- 移动真实文件
- 重命名文件
- 修改文件内容
- 修改普通文件图标
- 重启 Explorer

文件收纳功能是独立页面，执行前必须勾选文件并二次确认。遇到同名文件或路径冲突时会跳过，不覆盖、不删除。

## 系统要求

- Windows 10 / Windows 11 x64
- .NET 9 SDK，用于从源码构建
- Inno Setup 6，可选，用于生成安装包

## 从源码构建

克隆仓库：

```powershell
git clone https://github.com/SorrowGod/desktop-icon-manager.git
cd desktop-icon-manager
```

仓库结构：

```text
desktop-icon-manager/
├─ src/
│  └─ DesktopIconManager/      # WinForms 应用源码和 .csproj
├─ docs/                       # 使用说明、隐私说明、卸载说明、更新日志
├─ installer/                  # Inno Setup 安装包脚本和构建脚本
├─ update.json                 # GitHub Pages 使用的更新清单
├─ README.md
└─ LICENSE
```

编译：

```powershell
dotnet build .\src\DesktopIconManager\DesktopIconManager.csproj
```

发布单文件便携版：

```powershell
dotnet publish .\src\DesktopIconManager\DesktopIconManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64-manager-ui-fluent
```

生成安装包和便携 zip：

```powershell
.\installer\build-installer.ps1
```

构建脚本会读取 `DesktopIconManager.csproj` 中的版本号，并输出：

- `dist\DesktopIconManagerSetup-{version}-x64.exe`
- `dist\DesktopIconManagerPortable-{version}-x64.zip`

如果没有配置代码签名证书，构建不会失败，只会提示跳过签名。

## 可选代码签名

构建脚本支持这些环境变量：

```powershell
$env:DICM_SIGN_CERT_PATH = "C:\path\to\cert.pfx"
$env:DICM_SIGN_CERT_PASSWORD = "password"
$env:DICM_SIGN_TIMESTAMP_URL = "http://timestamp.digicert.com"
$env:DICM_SIGNTOOL_PATH = "C:\path\to\signtool.exe"
```

没有证书时仍可产出未签名安装包。

## 数据位置

用户数据保存在：

```text
%AppData%\DesktopIconManager
```

主要文件：

- `settings.json`：应用设置
- `profiles.json`：方案和场景
- `snapshots.json`：桌面布局快照
- `file-organize-operations.json`：文件收纳撤销记录
- `pending-operation.json`：未完成整理恢复记录
- `desktop-icon-manager.log`：本地日志

卸载程序只删除程序文件和启动快捷方式，不删除这些用户数据。

## 更新清单

`update.json` 是应用内检查更新读取的公开清单。示例：

```json
{
  "version": "1.1.5",
  "releaseDate": "2026-05-28",
  "downloadUrl": "https://github.com/SorrowGod/desktop-icon-manager/releases/download/v1.1.5/DesktopIconManagerSetup-1.1.5-x64.exe",
  "portableUrl": "https://github.com/SorrowGod/desktop-icon-manager/releases/download/v1.1.5/DesktopIconManagerPortable-1.1.5-x64.zip",
  "sha256": "5C1F2001E697D2F6FC510B70E36F80F2E2D65C6A19A1A9671CF08C9DC5D62DAC",
  "notes": [
    "修复部分电脑点击应用后桌面图标完全不移动但软件没有提示的问题"
  ]
}
```

应用只比较版本并提示下载，不会静默安装。

## 文档

- [使用说明](docs/使用说明.md)
- [更新日志](docs/CHANGELOG.md)
- [隐私说明](docs/PRIVACY.md)
- [卸载说明](docs/UNINSTALL.md)

## 许可证

本项目使用 MIT License。详见 [LICENSE](LICENSE)。
