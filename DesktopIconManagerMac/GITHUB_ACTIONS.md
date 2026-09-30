# GitHub Actions 构建 Mac 安装包

当前 Windows 机器不能安装 Xcode，也不能本地构建真正的 macOS `.app/.dmg`。仓库根目录的 `.github/workflows/build-mac.yml` 会使用 GitHub 提供的 `macos-latest` runner 构建 Mac 版。

## 使用方式

1. 把仓库推到 GitHub。
2. 打开 GitHub 仓库页面。
3. 进入 `Actions`。
4. 选择 `Build Mac`。
5. 点击 `Run workflow`。
6. 构建结束后，在运行详情页的 `Artifacts` 下载：
   - `DesktopIconManagerMac-dmg`
   - `DesktopIconManagerMac-app`

## 自动触发

以下文件变更后 push，会自动触发 Mac 构建：

- `DesktopIconManagerMac/**`
- `.github/workflows/build-mac.yml`

## 未签名构建

未配置 Apple Developer 证书时，workflow 会生成未签名、未公证的 `.dmg`。这种包适合内部测试，但普通用户打开时会遇到 Gatekeeper 拦截，需要右键打开或在系统设置中允许。

## 正式签名和公证

如果之后要正式发布，需要 Apple Developer Program 账号，并在 GitHub 仓库 `Settings > Secrets and variables > Actions` 中配置：

- `DEVELOPER_ID_APPLICATION`
- `APPLE_ID`
- `APPLE_TEAM_ID`
- `APPLE_APP_PASSWORD`

当前 `scripts/build-mac.sh` 已读取这些变量；配置后会尝试签名、公证并 stapler。

## 首次失败处理

Mac 版源码还没有在真实 macOS 上编译验证过。第一次 Actions 失败时，优先查看：

- `Verify package` 里的 Swift 编译/测试错误。
- `Build DMG` 里的 bundle、签名、`hdiutil` 错误。
- Finder 桌面图标移动能力必须下载到真实 Mac 上运行后验证。
