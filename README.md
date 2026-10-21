# ⚡️PCL 下载修复⚡️

> **本项目为第三方基于 [Plain Craft Launcher](https://github.com/Meloong-Git/PCL) 独立进行的二次创作，非官方版本。**
> 原作者与主要开发者：[龙腾猫跃](https://github.com/Meloong-Git)　·　赞助链接：[爱发电 · LTCat](https://meloong.com/afd/a/LTCat)
>
> 本仓库完整保留上游的 [LICENCE](LICENCE)（《PCL 分发有限许可》与《PCL 存储库合理使用指南》），请在使用前阅读。

---

PCL 的最新更新，不分地区关闭了下载 MC 的功能 😭，此非官方版指在让《留学生》可以直接使用此非官方版下载 Minecraft。

## 这是什么

在 PCL 官方版本中，`ModSecret.RestrictMcDownload` 是一个**编译期常量**，官方构建会因此把「不限制」的代码分支当作死代码剔除，导致**所有地区的用户都无法下载 Minecraft**。

本项目把该限制改为**运行时判断**：

- 启动时检测 Windows 的**首选语言**，若其中含任意中文（`zh-*`，含简体与繁体），则弹窗询问「你是中国用户吗？」
- 用户的回答写入配置文件 `PCL/Setup.ini` 的 `IsChinaUser` 项（`True` / `False` / `None`）
- **只有**用户明确选择「是，我是中国用户」时，才会限制 MC 下载功能
- 选择「否」或尚未作答，则**不限制**

配置文件中的取值：

| 值 | 含义 |
| --- | --- |
| `True` | 是中国用户 —— 限制 MC 下载 |
| `False` | 不是中国用户 —— 不限制 |
| `None` | 尚未选择 —— 每次启动时重新询问 |

## 改动内容

| 文件 | 说明 |
| --- | --- |
| `Plain Craft Launcher 2/Modules/Base/ModChinaUser.vb` | **新增**。首选语言检测、`IsChinaUser` 配置读写、启动询问逻辑 |
| `Plain Craft Launcher 2/Modules/ModSecret.vb` | `RestrictMcDownload` 由 `Const True` 改为动态属性，读取 `ModChinaUser.IsRestricted` |
| `Plain Craft Launcher 2/Pages/PageSetup/Settings.vb` | 新增 `IsChinaUser` 设置项 |
| `Plain Craft Launcher 2/FormMain.xaml.vb` | 在启动流程中调用 `ModChinaUser.StartupCheck()` |
| `Plain Craft Launcher 2/Pages/PageOther/PageOtherAbout.xaml` | 关于页面标注第三方二次创作 |
| `Plain Craft Launcher 2/Plain Craft Launcher 2.vbproj` | 注册新增的编译文件 |

## 编译方法

需要 .NET Framework 4.8 与 Visual Studio 2019+（或对应的 MS Build Tools）。

1. 克隆本仓库及其子模块：

   ```bash
   git clone --recursive https://github.com/XIGUA-114514/PCL-DownloadsFix.git
   cd PCL-DownloadsFix
   ```

2. 还原 NuGet 依赖：

   ```powershell
   dotnet restore PCLCS\PCLCS.csproj
   ```

3. 编译：

   ```powershell
   msbuild "Plain Craft Launcher 2\Plain Craft Launcher 2.vbproj" -p:Configuration=Release
   ```

   产物位于 `Plain Craft Launcher 2\bin\Plain Craft Launcher 2.exe`。

### 关于 CurseForge 与微软登录

上游开源版将敏感凭据留空，需要自行申请后通过**环境变量**注入，源码中不会包含任何密钥：

| 环境变量 | 用途 | 申请方式 |
| --- | --- | --- |
| `PCL_CURSEFORGE_API_KEY` | CurseForge API 调用（Mod 搜索、文件下载） | 在 [CurseForge for Studios 控制台](https://console.curseforge.com/) 添加 Minecraft 游戏后获取 |
| `PCL_MS_CLIENT_ID` | 微软正版账号登录 | 在 [Microsoft Entra 管理中心](https://entra.microsoft.com) 注册应用，获取 Application (client) ID |

```powershell
$env:PCL_CURSEFORGE_API_KEY = "你的 Key"
$env:PCL_MS_CLIENT_ID = "你的 Client ID"
.\Plain Craft Launcher 2.exe
```

## 免责声明

- 本项目**非官方**，与 [龙腾猫跃](https://github.com/Meloong-Git) 及 PCL 官方团队无关。
- 本项目遵循上游的《PCL 存储库合理使用指南》进行修改，并完整保留原许可文件。
- 使用者应自行确保其使用行为符合所在地法律法规，以及 CurseForge、Microsoft 等第三方服务的使用条款。
- 若你所在的地区禁止通过本项目获取 Minecraft，请自行停止使用。

## 相关链接

- 本项目 Issues（反馈与交流）：<https://github.com/XIGUA-114514/PCL-DownloadsFix/issues>
- PCL 官方仓库：<https://github.com/Meloong-Git/PCL>
- PCL 官方下载：<https://meloong.com/pcl>
- 帮助文档库：<https://github.com/LTCatt/PCL2Help>

> 请在本项目的 Issues 中反馈问题。上游仓库的讨论区仅用于 PCL 本身的问题，本项目为第三方二次创作，相关问题作者无法在此处理。
