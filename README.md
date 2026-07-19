# Infinite Martial Arts Mod for Hero's Path

一个用于《大侠立志传》的 BepInEx IL2CPP Mod。当前版本保留“无限武学”相关功能，并让主角、队友和我方友军已学习的全部内功都能触发各自的主内功战斗效果。
参考https://mod.3dmgame.com/mod/195081 和 https://www.bilibili.com/opus/847921213918412852
<img width="2560" height="1440" alt="4f8a2f8cfad81a5e31b7a2da80765dca" src="https://github.com/user-attachments/assets/4626e992-e654-47a5-868e-23cace65a737" />


## 功能简介

- 移除角色学习武功数量上限检查。
- 为已学习武功列表添加可滚动区域，超过可视范围后可以继续向下查看和点击。
- 为已学习武功条目添加“前置”按钮，方便把指定武功移动到列表前方。
- 主角、队友和我方友军已学习的全部内功，其主内功战斗效果无需设置即可同时生效。
- 敌人仍按原版规则只触发当前主内功；战斗外常驻属性不会重复结算。
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

仓库中包含三个源码辅助检查脚本：

```powershell
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
.\tests\check-all-internal-kungfu-effects.ps1
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
Loading [InfiniteMartialArts 1.1.0]
Infinite martial arts UI initialized.
```

## 目录结构

```text
src\EnhanceGameplay\EnhanceGameplay.csproj
src\EnhanceGameplay\EnhanceGameplay\BepInExLoader.cs
src\EnhanceGameplay\EnhanceGameplay\Bootstrapper.cs
src\EnhanceGameplay\EnhanceGameplay\InternalKungfuPatch.cs
src\EnhanceGameplay\EnhanceGameplay\MartialNumPatch.cs
src\EnhanceGameplay\EnhanceGameplay\ModComponent.cs
tests\
```

## 备注

插件 GUID 仍为 `com.haxx.EnhanceGameplay`，用于兼容原插件加载路径和配置位置；插件显示名称为 `InfiniteMartialArts`。

## ItemSpawner 独立物品生成器

`ItemSpawner` 是作者 Haxx 的独立 BepInEx 6 IL2CPP Mod，不依赖 `EnhanceGameplay.dll` 或 UniverseLib。

### 功能与使用

- 默认按 `F8` 打开或关闭物品生成器窗口。
- 按当前语言物品名称或物品 ID 实时检索；空搜索显示游戏当前加载的全部有效物品。
- 单次生成数量范围为 `1–999`，默认 `1`。
- 生成物品直接进入队伍背包，并使用游戏原生获得提示、堆叠和容量逻辑。
- 快捷键可在 `BepInEx/config/com.haxx.ItemSpawner.cfg` 中修改。

> 任务物品和隐藏物品也会显示，生成它们可能影响任务或存档进度。使用前请备份存档。

### 构建与检查

```powershell
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
.\tests\check-item-spawner-catalog.ps1
.\tests\check-item-spawner-grant.ps1
.\tests\check-item-spawner-ui.ps1
.\tests\check-item-spawner-targets.ps1 -PluginPath .\src\ItemSpawner\bin\Release\net6.0\ItemSpawner.dll
```

构建产物位于：

```text
src\ItemSpawner\bin\Release\net6.0\ItemSpawner.dll
```

### 安装

将构建出的 DLL 复制到：

```text
<游戏目录>\BepInEx\plugins\ItemSpawner\ItemSpawner.dll
```

默认游戏路径下的完整位置为：

```text
E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\plugins\ItemSpawner\ItemSpawner.dll
```

启动游戏后，`BepInEx/LogOutput.log` 中出现以下信息表示插件已加载：

```text
Loading [ItemSpawner 1.0.0]
ItemSpawner 1.0.0 loaded. Press F8 to toggle the window.
```

现有 `EnhanceGameplay` 回归检查仍可独立运行：

```powershell
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
.\tests\check-all-internal-kungfu-effects.ps1
.\tests\check-hook-targets.ps1 -PluginPath .\src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll
```
