# 路线图 Roadmap

第一阶段是这次交付的范围。后面的条目只列出来，不在这一版里实现。

Phase 1 is what this version ships. Later items are listed only.

## 第一阶段 Phase 1

- [x] 解决方案骨架：Core / Platform / WPF，依赖注入，MVVM
- [x] Solution skeleton: Core / Platform / WPF, dependency injection, MVVM
- [x] 应用自己的截图。全局热键默认 `Ctrl+Alt+S`（可配置；不用 QQ / 微信占用的 `Ctrl+Alt+A`）。`RegisterHotKey` 在 `IGlobalHotkey` 后面
- [x] In-app screenshot. Default global hotkey `Ctrl+Alt+S` (configurable; not `Ctrl+Alt+A`). `RegisterHotKey` sits behind `IGlobalHotkey`
- [x] 所有显示器上的暗色全屏选区。拖拽选择；Esc 或右键取消；回车或双击截取当前屏幕；显示物理像素尺寸
- [x] Dim full-screen region selector on every monitor. Drag to select; Esc or right-click cancels; Enter or double-click captures the current monitor; size is in physical pixels
- [x] 按 DPI 正确裁剪（物理像素），PNG 存到 `Pictures\Pegline`（否则 `%LOCALAPPDATA%\Pegline\Captures`）。截图在 `IScreenCapture` 后面
- [x] DPI-correct crop (physical pixels). PNG saved to `Pictures\Pegline` (else `%LOCALAPPDATA%\Pegline\Captures`). Capture sits behind `IScreenCapture`
- [x] 立刻挂到绳子上，并有一段飞向绳子的动画
- [x] Hung on the line immediately, with a short fly-to-line animation
- [x] 指针停在屏幕上沿时绳子滑下，离开后收起
- [x] The line glides down when the pointer rests on the top edge, and tucks away when it leaves
- [x] 带相框和夹子的缩略图
- [x] Framed thumbnails with a clip
- [x] 点击复制 PNG
- [x] Click to copy the PNG
- [x] X 只从绳子上取下，不删除文件
- [x] X removes the shot from the line and keeps the file
- [x] 托盘：显示/隐藏、退出
- [x] Tray: show/hide and quit
- [x] 另一热键 `Ctrl+Alt+L`（可配置）显示或隐藏绳子，托盘里也有同一项
- [x] A second hotkey, `Ctrl+Alt+L` (configurable), shows or hides the line. The tray has the same command
- [x] Core 单元测试
- [x] Core unit tests
- [x] Windows CI：构建、测试、自包含单文件 `win-x64` 产物
- [x] Windows CI: build, test, and a self-contained single-file `win-x64` artifact

## 以后 Later

- [ ] 可选：也把系统截图和复制的图片挂上来（截图文件夹、剪贴板）。复制图片时排除密码管理器（`ExcludeClipboardContentFromMonitorProcessing` / `CanIncludeInClipboardHistory`）
- [ ] Optional: also hang system screenshots and copied images (Screenshots folder, clipboard), excluding password-manager clipboard contents
- [ ] 把照片拖到其他应用或文件夹
- [ ] Drag a photo out to another app or a folder
- [ ] 双击打开
- [ ] Double-click to open
- [ ] 按住进行标注
- [ ] Press-and-hold markup
- [ ] 全屏时自动隐藏绳子
- [ ] Hide the line while another app is full screen
- [ ] 多显示器与 DPI 的摆放打磨（选区裁剪已经按物理像素映射，不依赖窗口坐标）
- [ ] Multi-monitor and DPI placement polish (cropping already maps physical pixels and does not depend on window coordinates)
- [ ] 绳子摆动动画
- [ ] Swing animation
- [ ] 屏幕录像与裁剪
- [ ] Screen recordings and trim
- [ ] 登录时启动
- [ ] Launch at login
- [ ] 界面本地化（resx，英文与简体中文）。这一版的界面文字是写死的双语
- [ ] UI localization (resx, English and zh-Hans). This version hardcodes bilingual strings
- [ ] 安装包与正式发布（CI 产物之外）
- [ ] Installer and a release beyond the CI artifact
