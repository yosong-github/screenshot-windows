# screenshot-windows

Screenshots hang from a line along the top of the screen. Press a shortcut, drag a region, and the picture is clipped to the line.

截图会挂在屏幕顶部的一根绳子上。按下快捷键，拖出区域，照片就夹到绳子上。

screenshot-windows is a Windows 10/11 app. It borrows the clothesline interaction (timings and layout proportions) from [Tendedero](https://github.com/alejandrobujan/tendedero) by Alejandro Buján, which is MIT-licensed. The name Tendedero and its icon are not used. See [LICENSE](LICENSE).

screenshot-windows 是 Windows 10/11 应用。晾衣绳的交互（时间与布局比例）来自 Alejandro Buján 的 [Tendedero](https://github.com/alejandrobujan/tendedero)（MIT）。本项目不使用 Tendedero 这个名字和图标。详见 [LICENSE](LICENSE)。

Renamed from Pegline. The first launch copies that `%LOCALAPPDATA%` folder into `screenshot-windows` when the new folder is not there yet. Pictures already on disk stay where they are; new captures use `Pictures\screenshot-windows`.

已改名。第一次启动时，若新的本地数据目录还不存在，会把原来的 `%LOCALAPPDATA%` 目录复制过去。已经保存的图片留在原处，新截图写入 `Pictures\screenshot-windows`。

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

- Touch the top edge and a small white handle slides down at the center of that screen. Rest on the handle and the line glides down. Leave the handle and it tucks away; leave the line and the line tucks away. An empty line does not open from the edge.
- 碰到屏幕上沿时，该屏幕顶部中央会滑下一小条白色把手。停在把手上，绳子才滑下来。离开把手后把手收起；离开绳子后绳子收起。绳子是空的时，靠上沿不会打开。
- Each shot is a framed thumbnail with a clip. Click copies the PNG. The X takes it off the line and leaves the file in place.
- 每张图是带相框和夹子的缩略图。点击复制 PNG。X 只从绳子上取下，文件还在。
- The line keeps eight shots. An older one drops off the line when a ninth arrives. The PNG stays in the folder.
- 绳子上最多 8 张。第九张到来时，最旧的离开绳子，PNG 仍留在文件夹里。
- After a capture, the new picture flies onto the line and the line stays down briefly.
- 截图之后，新照片会飞到绳子上，绳子会短暂保持展开。

Pictures are saved under `Pictures\screenshot-windows` (or `%LOCALAPPDATA%\screenshot-windows\Captures` when there is no Pictures folder). Settings live in `%LOCALAPPDATA%\screenshot-windows\settings.json`.

图片保存在 `Pictures\screenshot-windows`（没有图片文件夹时用 `%LOCALAPPDATA%\screenshot-windows\Captures`）。设置在 `%LOCALAPPDATA%\screenshot-windows\settings.json`。

The tray menu can show or hide the line, take a screenshot, edit shortcuts, turn on 开机自启, and quit. A second launch exits.

托盘可以显示或隐藏绳子、截图、改快捷键、开关开机自启、退出。再次启动会直接退出。

## Install 安装

The Windows CI run uploads two artifacts. `screenshot-windows-win-x64` is the portable `screenshot-windows.exe`. `screenshot-windows-setup` is `screenshot-windows-setup-<version>.exe`, built with Inno Setup. The version is `<Version>` in `Directory.Build.props`, the same version stamped on the executable.

Windows CI 上传两个产物。`screenshot-windows-win-x64` 是便携的 `screenshot-windows.exe`。`screenshot-windows-setup` 是 Inno Setup 打出来的 `screenshot-windows-setup-<version>.exe`。版本号是 `Directory.Build.props` 里的 `<Version>`，可执行文件用的也是这个版本。

The installer is per user. It does not ask for an administrator. Files go to `%LOCALAPPDATA%\Programs\screenshot-windows`. The wizard is English or Simplified Chinese. It always adds a Start menu shortcut, and offers an optional desktop shortcut. 开机自启 is checked the first time. Finish can launch the app.

安装包是按当前用户安装的，不会弹出管理员确认。文件放在 `%LOCALAPPDATA%\Programs\screenshot-windows`。向导有英文和简体中文。开始菜单快捷方式总会创建，桌面快捷方式可选。第一次安装时开机自启默认勾选。结束时可以立即启动。

Browsers may warn about the unsigned download. CI signs the portable exe and the installer only when Azure Trusted Signing secrets are set: `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_CODESIGNING_ENDPOINT`, `AZURE_CODESIGNING_ACCOUNT`, and `AZURE_CODESIGNING_PROFILE`. When any of those is absent, signing is skipped and the build still succeeds.

浏览器可能会把未签名的下载标成危险。只有配齐 Azure Trusted Signing 的密钥时，CI 才会给便携 exe 和安装包签名：`AZURE_TENANT_ID`、`AZURE_CLIENT_ID`、`AZURE_CLIENT_SECRET`、`AZURE_CODESIGNING_ENDPOINT`、`AZURE_CODESIGNING_ACCOUNT`、`AZURE_CODESIGNING_PROFILE`。缺任何一个就跳过签名，构建仍然成功。

## Launch at login 开机自启

The tray item **开机自启** reads and writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value name `screenshot-windows`. The command is the quoted path of the running executable plus `--autostart`. No administrator rights. The checkmark is the registry value, not a separate setting.

托盘里的 **开机自启** 读写 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`，值名是 `screenshot-windows`。命令是当前可执行文件的带引号路径，再加 `--autostart`。不需要管理员。勾选状态就是注册表里的值，不是另一份设置。

`--autostart` starts in the tray and does not drop the line. A second copy still exits. If startup is on and the Run value points at a different path, the next launch rewrites it to this executable.

`--autostart` 只留在托盘里，不会把绳子放下来。再开一个实例仍会退出。如果开机自启开着，但 Run 值指向别的路径，下次启动会改成当前这个程序。

## Uninstall 卸载

Uninstall from Windows Settings or the Start menu entry. The uninstaller closes screenshot-windows, removes shortcuts, and deletes the Run value. It then asks whether to delete `%LOCALAPPDATA%\screenshot-windows` (settings, and captures stored there when there is no Pictures folder). `Pictures\screenshot-windows` is never deleted. A silent uninstall keeps the local data folder.

在 Windows 设置或开始菜单里卸载。卸载程序会先关掉 screenshot-windows，删掉快捷方式和 Run 值，再询问要不要删除 `%LOCALAPPDATA%\screenshot-windows`（设置，以及没有图片文件夹时存在那里的截图）。`Pictures\screenshot-windows` 不会被删除。静默卸载会保留本地数据目录。

## Layout 结构

```
ScreenshotWindows.App          WPF, MVVM, Microsoft.Extensions.Hosting
    -> ScreenshotWindows.Platform    Win32 behind the Core interfaces
        -> ScreenshotWindows.Core    domain and contracts, no Win32
ScreenshotWindows.Core.Tests -> ScreenshotWindows.Core
```

Platform contracts include `IScreenCapture` (freeze and crop), `IGlobalHotkey` (`RegisterHotKey`), `IDisplayEnvironment`, `IClipboardImageWriter`, `ICaptureStore`, and `ISettingsStore`. `IAutostart` lives in Platform and writes the HKCU Run value. The command text is `LoginAutostart` in Core, tested with a fake store.

平台接口包括 `IScreenCapture`（冻结与裁剪）、`IGlobalHotkey`（`RegisterHotKey`）、`IDisplayEnvironment`、`IClipboardImageWriter`、`ICaptureStore` 和 `ISettingsStore`。`IAutostart` 在 Platform 里，负责写 HKCU Run。命令文本由 Core 的 `LoginAutostart` 决定，测试用假的存储。

Phase 1 is the capture hotkey, the region overlay, and the clothesline. Later work is listed in [ROADMAP.md](ROADMAP.md). The app does not watch the system Screenshots folder or the clipboard.

第一阶段是截图热键、选区遮罩和晾衣绳。后续见 [ROADMAP.md](ROADMAP.md)。本阶段不监视系统截图文件夹，也不监视剪贴板。

## Build 构建

.NET 8 SDK. The Windows app targets `net8.0-windows`. `EnableWindowsTargeting` lets `dotnet build` and `dotnet test` run on Linux; the UI still needs Windows.

需要 .NET 8 SDK。应用目标框架是 `net8.0-windows`。`EnableWindowsTargeting` 让 Linux 上也能 `dotnet build` 和 `dotnet test`；界面本身要在 Windows 上运行。

```bash
dotnet build screenshot-windows.sln -c Release
dotnet test screenshot-windows.sln -c Release
dotnet run --project src/ScreenshotWindows.App -c Release
```

Self-contained single-file executable:

自包含单文件：

```bash
dotnet publish src/ScreenshotWindows.App/ScreenshotWindows.App.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

GitHub Actions on `windows-latest` builds, tests, and uploads `screenshot-windows.exe` plus `screenshot-windows-setup-<version>.exe`.

GitHub Actions 在 `windows-latest` 上构建、测试，并上传 `screenshot-windows.exe` 和 `screenshot-windows-setup-<version>.exe`。

## What this environment did not run 这里没能实际跑起来的部分

This workspace is Linux. The solution builds here, and the Core tests pass. These were not executed on Windows: the global hotkey, BitBlt capture, per-monitor DPI placement, the region overlay, the clothesline window, the fly-to-line animation, the tray icon, copying an image to the clipboard, the HKCU Run value, and the Inno Setup installer.

当前环境是 Linux。解决方案可以在这里编译，Core 测试可以通过。以下没有在 Windows 上执行过：全局热键、BitBlt 截图、按显示器 DPI 摆放窗口、选区遮罩、晾衣绳窗口、飞向绳子的动画、托盘图标、把图片写入剪贴板、HKCU Run 值，以及 Inno Setup 安装包。
