# Infinite Martial Arts Mod for Hero's Path

一个用于《大侠立志传》的 BepInEx IL2CPP Mod。当前版本只保留“无限武学”相关功能：允许角色学习超过原版数量上限的武功，并修复武学面板在武功数量超出可视区域后无法继续向下滚动、无法点击下方武功的问题。

## 功能简介

- 移除角色学习武功数量上限检查。
- 为已学习武功列表添加可滚动区域，超过可视范围后可以继续向下查看和点击。
- 为已学习武功条目添加“前置”按钮，方便把指定武功移动到列表前方。
- 不包含工具不损坏、送礼限制、简单 QTE、战斗倍速、跑图加速等其它功能。

## 构建说明

### 环境要求

- Windows
- .NET SDK 6.0 或更高版本
- 已安装《大侠立志传》
- 已为游戏安装 BepInEx 6 IL2CPP，并生成 `BepInEx/core` 与 `BepInEx/interop` 依赖文件
- `UniverseLib.IL2CPP.Interop.dll` 位于游戏的 `BepInEx/plugins/EnhanceGameplay` 目录中

项目文件中的引用路径当前指向：

```text
E:\SteamLibrary\steamapps\common\WulinSH
```

如果你的游戏安装路径不同，需要先修改 [EnhanceGameplay.csproj](src/EnhanceGameplay/EnhanceGameplay.csproj) 中各个 `HintPath`。

### 构建命令

在仓库根目录运行：

```powershell
dotnet build .\src\EnhanceGameplay\EnhanceGameplay.csproj -c Release
```

构建产物位于：

```text
src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll
```

### 可选检查

仓库中包含两个辅助检查脚本：

```powershell
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
```

如果本机游戏路径与默认路径一致，也可以检查 Harmony 目标方法是否还能解析：

```powershell
.\tests\check-hook-targets.ps1 -PluginPath .\src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll
```

## Mod 安装说明

1. 确认游戏已安装 BepInEx 6 IL2CPP。
2. 构建本项目，得到 `EnhanceGameplay.dll`。
3. 在游戏目录下创建或确认存在以下目录：

```text
BepInEx\plugins\EnhanceGameplay
```

4. 将构建出的 DLL 复制到：

```text
<游戏目录>\BepInEx\plugins\EnhanceGameplay\EnhanceGameplay.dll
```

例如：

```text
E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\plugins\EnhanceGameplay\EnhanceGameplay.dll
```

5. 启动游戏后打开 `BepInEx\LogOutput.log`，看到类似日志即表示插件已加载：

```text
Loading [InfiniteMartialArts 1.0.0]
Infinite martial arts UI initialized.
```

## 目录结构

```text
src\EnhanceGameplay\EnhanceGameplay.csproj
src\EnhanceGameplay\EnhanceGameplay\BepInExLoader.cs
src\EnhanceGameplay\EnhanceGameplay\Bootstrapper.cs
src\EnhanceGameplay\EnhanceGameplay\MartialNumPatch.cs
src\EnhanceGameplay\EnhanceGameplay\ModComponent.cs
tests\
```

## 备注

插件 GUID 仍为 `com.haxx.EnhanceGameplay`，用于兼容原插件加载路径和配置位置；插件显示名称为 `InfiniteMartialArts`。
