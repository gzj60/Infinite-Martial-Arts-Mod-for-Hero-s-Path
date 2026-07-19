# 游戏内物品生成器设计

## 背景

当前仓库包含《大侠立志传》的 `EnhanceGameplay` BepInEx 6 IL2CPP Mod。本功能不并入现有插件，而是新增一个完全独立的 `ItemSpawner` Mod。玩家通过快捷键打开游戏内窗口，检索游戏当前已加载的全部物品数据，并把指定数量的物品加入队伍背包。

已核实当前游戏交互程序集提供以下稳定入口：

- `BaseDataClass.GetGameData<GameData.ItemDataScriptObject>()`：取得当前游戏数据表。
- `GameData.ItemDataScriptObject.ItemData`：枚举当前已加载的物品记录。
- `WuLin.GameUtil.GetName(GameData.ItemData, bool)`：取得当前语言下的物品名称。
- `WuLin.PlayerTeamManager.Instance.TeamInventory`：取得玩家队伍背包。
- `WuLin.GameItemPack.AddItem(int, int, bool)`：把指定物品加入临时物品包；第三个参数是 `needCheck`，生成任务物品时必须为 `false`。
- `WuLin.PlayerTeamManager.PickupPack(GameItemPack, ...)`：把临时物品包交给队伍背包，并显示游戏原生获得提示。

## 目标

- 生成独立的 `ItemSpawner.dll`，不修改或依赖 `EnhanceGameplay.dll`。
- 默认按 `F8` 打开或关闭物品生成窗口，快捷键可通过 BepInEx 配置修改。
- 显示当前游戏数据表中的全部有效物品记录，包括任务、隐藏和已加载的 DLC 物品。
- 支持按当前语言名称或物品 ID 实时检索。
- 允许输入 `1–999` 的生成数量，默认值为 `1`。
- 把物品加入 `PlayerTeamManager.TeamInventory`，并使用游戏原生物品获得提示。
- 在常见分辨率下提供可拖动、可滚动、支持中文输入的 UGUI 窗口。
- 游戏状态未就绪或生成失败时给出明确反馈，不影响游戏继续运行。

## 非目标

- 不修改物品模板、掉落表、商店、任务条件或存档格式。
- 不提供类型、品级、内部名称等高级筛选；内部名称只作为缺少本地化名称时的显示回退。
- 不过滤可能影响任务或进度的物品，但窗口必须提示相关存档风险。
- 不提供删除物品、修改装备词条、批量生成预设或收藏夹。
- 不暂停游戏，也不永久改变鼠标可见性或锁定状态。
- 不依赖或打包 UniverseLib。

## 方案比较

### 方案 A：独立原生 UGUI 窗口（采用）

运行时使用游戏已有的 Unity UI 和 TextMeshPro 程序化创建窗口。窗口拥有搜索框、物品列表、数量输入框、生成按钮和状态栏。

优点是中文输入和焦点行为优于 IMGUI，界面可扩展，同时只依赖游戏已有程序集。代价是需要显式创建 Canvas、事件、滚动区域和行复用逻辑。

### 方案 B：Unity IMGUI

使用 `OnGUI` 绘制窗口。实现量较小，但视觉粗糙，中文输入法、控件焦点和长列表滚动体验较差，不采用。

### 方案 C：注入原生背包界面

向游戏背包界面增加生成入口。视觉最接近原版，但强依赖具体 UI 层级和预制体结构，对游戏更新敏感，也会把修改器功能混入正常背包流程，不采用。

## 项目与组件结构

新增独立项目 `src/ItemSpawner/ItemSpawner.csproj`，程序集名为 `ItemSpawner`，目标框架沿用现有环境的 `net6.0`。

### `ItemSpawnerPlugin`

- 提供独立的 BepInEx GUID、名称和 `1.0.0` 版本。
- 绑定默认值为 `F8` 的可配置快捷键。
- 注册所需 IL2CPP MonoBehaviour 类型并创建持久运行时对象。
- 将未处理异常写入独立插件日志。

### `ItemSpawnerController`

- 每帧检测配置快捷键并切换窗口。
- 等待游戏数据和玩家队伍状态就绪。
- 协调目录加载、搜索、选择与生成动作。
- 打开窗口时保存鼠标可见性和锁定状态，显示并解锁鼠标；关闭时恢复保存的状态。
- 不改变 `Time.timeScale`。

### `ItemCatalog`

- 通过 `BaseDataClass.GetGameData<GameData.ItemDataScriptObject>()` 获取数据表。
- 遍历 `ItemData` 数组，跳过空引用和缺少有效 ID 的损坏记录，不按类型、来源或可获得性过滤。
- 为每条记录缓存模板引用、ID、当前语言名称和内部名称。
- 使用 `GameUtil.GetName(item, false)` 获取显示名称；本地化名称为空时回退到 `UName`，仍为空时显示“未命名物品”。
- 搜索为不区分大小写的名称包含匹配和物品 ID 文本包含匹配；空字符串返回全部记录。
- 结果按物品 ID 升序排列。相同名称的物品保留为不同 ID 的独立行。
- 数据表未就绪时返回可重试状态，不缓存永久失败。

### `ItemSpawnerWindow`

- 创建屏幕空间覆盖 Canvas、GraphicRaycaster，并复用现有 EventSystem；没有 EventSystem 时创建所需实例。
- 使用游戏当前可用的 TextMeshPro 中文字体资源，避免中文显示为方框。
- 使用 `CanvasScaler.ScaleWithScreenSize` 适配不同分辨率。
- 提供标题栏、关闭按钮、搜索输入框、可滚动物品列表、数量输入框、生成按钮、状态栏和存档风险提示。
- 物品行显示 ID 与名称；当前选择使用高亮状态。
- 使用固定数量的可见行并随滚动位置复用，避免为全部物品同时创建 GameObject。
- 窗口可拖动，但拖动后必须保持至少标题栏处于屏幕范围内。
- `F8`、右上角关闭按钮或 `Esc` 可关闭窗口。

### `ItemGrantService`

- 验证已选择有效物品。
- 严格解析十进制数量，并限制到 `1–999`；非法输入不调用游戏接口。
- 验证 `PlayerTeamManager.Instance` 和 `TeamInventory` 已就绪。
- 在 Unity 主线程创建临时 `GameItemPack`，调用 `pack.AddItem(item.Uid, quantity, false)`，避免任务物品被获取检查拒绝。
- 以 `AddItem` 返回值判断物品数据是否有效；成功后调用 `PlayerTeamManager.Instance.PickupPack(pack)` 完成原生入包和获得提示。
- 成功后保留当前选择和数量，便于连续生成；堆叠、独立装备实例、红点和提示交给原生领取流程。

## 窗口布局与交互

```text
┌────────────── 物品生成器 ────────────── × ┐
│ 搜索名称或 ID：[______________________] │
├─────────────────────────────────────────┤
│ 10001   碎银                            │
│ 10002   金疮药                          │
│ 10003   无名剑                          │
│ ...可滚动，当前选择高亮...               │
├─────────────────────────────────────────┤
│ 数量：[ 1 ]  范围 1–999   [生成物品]    │
│ 状态：已获得“金疮药”×1                  │
│ 提示：任务或隐藏物品可能影响存档。       │
└─────────────────────────────────────────┘
```

- 搜索内容变化后立即刷新结果，不需要额外确认。
- 搜索框为空时显示全部记录。
- 点击列表行只改变选择，不立即生成。
- 生成按钮仅在物品、数量和游戏状态都有效时可执行。
- 成功后状态栏显示物品名称与数量，并由游戏显示原生获得提示。
- 窗口关闭后不保留输入焦点；同一游戏进程内再次打开时保留上次搜索、选择和数量。

## 数据流

1. BepInEx 加载 `ItemSpawnerPlugin`，绑定快捷键配置并创建控制器。
2. 玩家按下 `F8`。
3. 控制器保存鼠标状态，显示窗口，并请求 `ItemCatalog` 初始化。
4. 目录服务取得 `ItemDataScriptObject.ItemData`，解析名称并建立按 ID 排序的缓存。
5. 窗口根据搜索文本生成过滤结果，滚动列表只绑定当前可见行。
6. 玩家选择物品并输入数量。
7. `ItemGrantService` 验证输入和游戏运行时状态。
8. 服务创建临时物品包并调用 `pack.AddItem(id, quantity, false)`。
9. 服务调用 `PlayerTeamManager.Instance.PickupPack(pack)`；原生背包处理堆叠、红点和获得提示，窗口显示调用结果。
10. 玩家关闭窗口，控制器恢复鼠标状态。

## 错误与边界处理

- 游戏尚未进入存档、数据表未加载或队伍背包为空时，生成按钮不可执行，状态栏显示“游戏数据尚未就绪”，之后打开窗口或定时刷新时自动重试。
- 物品记录为空或 ID 无效时不加入目录，并记录调试日志；其它记录继续加载。
- 名称解析单条失败时使用内部名称或“未命名物品”，不放弃整个目录。
- 数量为空、非整数、小于 `1` 或大于 `999` 时显示输入错误，不自动改成其它值。
- 临时物品包的 `AddItem` 返回 `false` 时显示“生成失败，物品数据无效”。
- 调用抛出异常时记录完整 BepInEx 错误日志，窗口只显示简短错误，插件保持可用。
- 关闭窗口和销毁插件对象都必须恢复鼠标状态。
- 任务、隐藏和 DLC 物品按要求展示；插件只保证显示当前游戏进程实际加载到数据表中的 DLC 记录。

## 测试与验证

### 自动化检查

- 先增加失败的 `tests/check-item-spawner-source.ps1`，检查：
  - 插件拥有独立 GUID 和程序集，未修改 `EnhanceGameplay` 注册逻辑。
  - 默认快捷键为 `F8` 且通过 BepInEx 配置绑定。
  - 目录来自 `ItemDataScriptObject.ItemData`，名称来自 `GameUtil.GetName`。
  - 搜索覆盖名称和 ID，结果按 ID 排序。
  - 数量验证明确限制为 `1–999`。
  - 生成先调用临时物品包的 `AddItem(id, quantity, false)`，再通过 `PlayerTeamManager.Instance.PickupPack` 完成原生领取。
  - 鼠标状态在关闭路径恢复，且不修改 `Time.timeScale`。
- 增加 `tests/check-item-spawner-targets.ps1`，通过反射验证当前游戏程序集仍包含设计依赖的类型、属性和方法，并检查构建 DLL 的 BepInEx 元数据可解析。
- 运行现有 `EnhanceGameplay` 检查，确认新增独立项目没有回归现有插件。
- Release 构建必须为零警告、零错误，`git diff --check` 无输出。

### 游戏内验收

1. 进入已有存档，按 `F8` 打开和关闭窗口。
2. 验证中文字体、拖动、不同分辨率缩放与滚动列表。
3. 分别按完整/部分中文名称和完整/部分 ID 搜索。
4. 验证任务、隐藏和已加载 DLC 物品没有被类型过滤。
5. 以数量 `1` 和 `999` 生成可堆叠物品，确认队伍背包数量和原生提示。
6. 生成不可堆叠装备，确认原生背包容量规则仍生效。
7. 验证空值、非数字、`0` 和 `1000` 被阻止。
8. 在未进入存档的状态打开窗口，确认不会抛出异常或生成物品。
9. 关闭窗口后确认鼠标可见性和锁定状态恢复。
10. 检查 `BepInEx/LogOutput.log` 没有未处理异常。

## 版本、安装与交付

- 初始版本为 `1.0.0`。
- 默认安装目录为 `BepInEx/plugins/ItemSpawner/ItemSpawner.dll`。
- 插件只依赖游戏已安装的 BepInEx、Il2CppInterop、Unity UI、TextMeshPro、`Assembly-CSharp` 和 `ModShare.Runtime` 程序集。
- 构建后先运行全部自动化检查，再为本机同名 DLL 创建时间戳备份并部署。
- 部署后比较构建产物和安装 DLL 的 SHA-256，确保文件一致。
- 生成可直接解压到游戏根目录的发布包，包含 `BepInEx/plugins/ItemSpawner/ItemSpawner.dll` 和安装说明。
- 不自动推送远程仓库或创建 GitHub Release。
