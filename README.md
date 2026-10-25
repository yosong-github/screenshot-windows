# Pegline

Screenshots hang from a line along the top of the screen. Press a shortcut, drag a region, and the picture is clipped to the line.

截图会挂在屏幕顶部的一根绳子上。按下快捷键，拖出区域，照片就夹到绳子上。

Pegline is a Windows 10/11 app. It borrows the clothesline interaction (timings and layout proportions) from [Tendedero](https://github.com/alejandrobujan/tendedero) by Alejandro Buján, which is MIT-licensed. The name Tendedero and its icon are not used. See [LICENSE](LICENSE).

Pegline 是 Windows 10/11 应用。晾衣绳的交互（时间与布局比例）来自 Alejandro Buján 的 [Tendedero](https://github.com/alejandrobujan/tendedero)（MIT）。本项目不使用 Tendedero 这个名字和图标。详见 [LICENSE](LICENSE)。

There is no network access and no telemetry.

没有网络请求，也没有遥测。

## Shortcuts 快捷键

| Action | Default | 说明 |
| --- | --- | --- |
| Take a screenshot 截图 | `Ctrl+Alt+S` | Freezes every monitor, then drag a region. 冻结所有屏幕后拖选区域。 |
| Show or hide the line 显示或隐藏绳子 | `Ctrl+Alt+L` | Also on the tray menu. 托盘菜单里也有。 |

Both chords are configurable from the tray item **Shortcuts… / 快捷键…**. The capture chord cannot be cleared. The line chord can (Backspace while recording it).

`Ctrl+Alt+A` is the screenshot chord used by QQ and WeChat, so it is intentionally not the default.

两个快捷键都可以在托盘的 **Shortcuts… / 快捷键…** 里修改。截图快捷键不能清除。绳子快捷键可以（录制时按退格）。

QQ 和微信占用了 `Ctrl+Alt+A`，所以默认不用这个组合。

On the overlay: drag to select, Enter or double-click captures the current monitor, Esc or right-click cancels. The size label is in physical pixels.

选区上：拖拽选择；回车或双击截取当前屏幕；Esc 或右键取消。尺寸是物理像素。

## What the line does 绳子怎么用

- Rest the pointer on the top edge for a moment and the line glides down. Leave and it tucks away. An empty line does not open from the edge.
- 指针在屏幕上沿停一会儿，绳子滑下来；离开后收起。绳子是空的时，靠上沿不会打开。
- Each shot is a framed thumbnail with a clip. Click copies the PNG. The X takes it off the line and leaves the file in place.
- 每张图是带相框和夹子的缩略图。点击复制 PNG。X 只从绳子上取下，文件还在。
- The line keeps eight shots. An older one drops off the line when a ninth arrives. The PNG stays in the folder.
- 绳子上最多 8 张。第九张到来时，最旧的离开绳子，PNG 仍留在文件夹里。
- After a capture, the new picture flies onto the line and the line stays down briefly.
- 截图之后，新照片会飞到绳子上，绳子会短暂保持展开。

Pictures are saved under `Pictures\Pegline` (or `%LOCALAPPDATA%\Pegline\Captures` when there is no Pictures folder). Settings live in `%LOCALAPPDATA%\Pegline\settings.json`.

图片保存在 `Pictures\Pegline`（没有图片文件夹时用 `%LOCALAPPDATA%\Pegline\Captures`）。设置在 `%LOCALAPPDATA%\Pegline\settings.json`。

The tray menu can show or hide the line, take a screenshot, edit shortcuts, and quit. A second launch exits.

托盘可以显示或隐藏绳子、截图、改快捷键、退出。再次启动会直接退出。

## Layout 结构

```
Pegline.App          WPF, MVVM, Microsoft.Extensions.Hosting
    -> Pegline.Platform    Win32 behind the Core interfaces
        -> Pegline.Core    domain and contracts, no Win32
Pegline.Core.Tests -> Pegline.Core
```

Platform contracts include `IScreenCapture` (freeze and crop), `IGlobalHotkey` (`RegisterHotKey`), `IDisplayEnvironment`, `IClipboardImageWriter`, `ICaptureStore`, and `ISettingsStore`.

平台接口包括 `IScreenCapture`（冻结与裁剪）、`IGlobalHotkey`（`RegisterHotKey`）、`IDisplayEnvironment`、`IClipboardImageWriter`、`ICaptureStore` 和 `ISettingsStore`。

Phase 1 is the capture hotkey, the region overlay, and the clothesline. Later work is listed in [ROADMAP.md](ROADMAP.md). The app does not watch the system Screenshots folder or the clipboard.

第一阶段是截图热键、选区遮罩和晾衣绳。后续见 [ROADMAP.md](ROADMAP.md)。本阶段不监视系统截图文件夹，也不监视剪贴板。

## Build 构建

.NET 8 SDK. The Windows app targets `net8.0-windows`. `EnableWindowsTargeting` lets `dotnet build` and `dotnet test` run on Linux; the UI still needs Windows.

需要 .NET 8 SDK。应用目标框架是 `net8.0-windows`。`EnableWindowsTargeting` 让 Linux 上也能 `dotnet build` 和 `dotnet test`；界面本身要在 Windows 上运行。

```bash
dotnet build Pegline.sln -c Release
dotnet test Pegline.sln -c Release
dotnet run --project src/Pegline.App -c Release
```

Self-contained single-file executable:

自包含单文件：

```bash
dotnet publish src/Pegline.App/Pegline.App.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

GitHub Actions on `windows-latest` builds, tests, and uploads `Pegline.exe`.

GitHub Actions 在 `windows-latest` 上构建、测试并上传 `Pegline.exe`。

## What this environment did not run 这里没能实际跑起来的部分

This workspace is Linux. The solution builds here, and the Core tests pass. These were not executed on Windows: the global hotkey, BitBlt capture, per-monitor DPI placement, the region overlay, the clothesline window, the fly-to-line animation, the tray icon, and copying an image to the clipboard.

当前环境是 Linux。解决方案可以在这里编译，Core 测试可以通过。以下没有在 Windows 上执行过：全局热键、BitBlt 截图、按显示器 DPI 摆放窗口、选区遮罩、晾衣绳窗口、飞向绳子的动画、托盘图标，以及把图片写入剪贴板。
