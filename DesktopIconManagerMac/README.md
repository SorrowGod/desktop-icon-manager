# 桌面管理器 Mac

这是桌面管理器的原生 macOS 版本工程，和 Windows 版保持同一个产品名、版本号、更新清单语义和核心布局规则，但不与 Windows WinForms 工程合并。

## 技术栈

- SwiftUI：主界面、设置、预览、文件收纳、诊断更新。
- AppKit：屏幕工作区、Finder 自动化、文件查看器、macOS 应用生命周期。
- LaunchAgent：开机自动整理。
- AppleScript/Finder：尝试设置桌面图标位置。

## 功能状态

- 已实现：桌面文件读取、布局预览、左/右/上/下/中间/分区/自定义图案布局计算、快照、文件收纳、撤销收纳、设置保存、更新检查、诊断包、LaunchAgent。
- 可用但需要真机验证：Finder 桌面图标坐标移动。首次应用布局时，macOS 会要求允许本应用控制 Finder。
- 文字轮廓和图片蒙版使用 AppKit 位图采样生成布局点位；Finder 实际吸附效果仍需真机验证。

## 构建

```bash
cd DesktopIconManagerMac
swift test
./scripts/build-mac.sh
```

输出：

- `.build/mac-app/桌面管理器.app`
- `dist/DesktopIconManagerMac-1.1.5-universal.dmg`

没有 Mac 时，可以用 GitHub Actions 构建。说明见 `GITHUB_ACTIONS.md`，workflow 位于仓库根目录 `.github/workflows/build-mac.yml`。

## 签名和公证

未配置证书时脚本会使用临时签名生成未公证 dmg，适合内部测试。正式分发需要在 Mac 上导入 Developer ID Application 证书，然后设置：

```bash
export DEVELOPER_ID_APPLICATION="Developer ID Application: Your Name (TEAMID)"
export APPLE_ID="you@example.com"
export APPLE_TEAM_ID="TEAMID"
export APPLE_APP_PASSWORD="app-specific-password"
./scripts/build-mac.sh
```

## 权限

Mac 版需要：

- 桌面目录访问权限：读取桌面文件、生成收纳建议。
- 自动化权限：控制 Finder 调整桌面图标位置。

正式签名时会加入 Apple Events 权限声明；首次控制 Finder 仍需用户在 macOS 授权。

如果系统拒绝 Finder 自动化，应用会保留预览和文件收纳，但会禁用布局应用与快照恢复，避免使用未知坐标覆盖桌面位置。
