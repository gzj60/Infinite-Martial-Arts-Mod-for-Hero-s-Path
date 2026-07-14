# 我方全内功主内功效果设计

## 背景

游戏角色可以学习多本内功，但只保存一个当前主内功。战斗中的主内功特殊效果由 `BattleActor.CreateInternalKungfuEffectEvents` 在七个 `BattleInternalKungfu*` 阶段生成，因此未设置为主内功的内功不会触发这些效果。

本功能让主角、队友和我方友军的全部已学内功都能触发各自的主内功战斗效果，同时保持敌方、战斗外属性和存档数据不变。

## 目标

- 主角、队友及我方友军无需切换主内功，全部已学内功的主内功效果都可在战斗中生效。
- 覆盖进入战斗、攻击前后、受击前后、行动结束和战斗中切换内功七个原版触发阶段。
- 当前主内功保持原版触发顺序，其他内功按角色内功列表顺序追加。
- 敌方角色完全保持原版只触发当前主内功的行为。
- 不修改角色存档中的当前主内功，不要求玩家先手动切换一次。
- 单个内功生成效果失败时不影响当前主内功和其他内功继续结算。

## 非目标

- 不重复结算学会或升级内功获得的生命、内力、防御等常驻属性；这些属性由 `GameCharacterInstance.ComputeKungfuProp` 处理，不属于主内功特殊效果。
- 不修改敌人、临时敌对角色或当前服务于敌方阵营的角色。
- 不移除游戏对全局唯一效果、同类效果或单次触发效果的原有限制。
- 不改变武学数量上限、滚动面板和“前置”按钮的既有行为。
- 本次实现不自动创建或覆盖 GitHub Release。

## 方案比较

### 方案 A：扩展原版主内功事件链（采用）

在 `BattleActor.CreateInternalKungfuEffectEvents` 的 Harmony 后置补丁中保留原版结果，并为其他已学内功调用相同的原版事件生成逻辑。新事件通过 `BattleFieldEvent.LinkWith` 追加到原事件链。

优点是复用游戏现有条件、动作和事件顺序，不需要重写各内功效果，也不修改存档。主要风险是递归调用和临时主内功状态，使用线程级递归保护与 `try/finally` 恢复解决。

### 方案 B：永久激活所有内功动态修改器

在角色学习或切换内功时永久登记所有内功效果。这种方式接近部分数据 Mod 的做法，但可能写入存档、要求先切换内功，并可能让战斗外阶段误触发，不符合本需求。

### 方案 C：重放每个战斗回调

分别补丁进入战斗、攻击、受击和行动结束等回调，再手工生成内功效果。该方式触点多、容易遗漏，且对游戏更新更敏感，不采用。

## 代码结构

新增 `InternalKungfuPatch.cs`，职责限定为全内功战斗效果：

- `CreateInternalKungfuEffectEvents_Postfix`：Harmony 入口，检查阶段、阵营和递归状态。
- `IsFriendlyActor`：仅接受当前服务阵营为 `BattleTeamEnum.Player` 或 `BattleTeamEnum.Allie` 的角色。
- `IsInternalKungfuBattleStage`：仅接受七个 `BattleInternalKungfu*` 阶段。
- `BuildAdditionalEvents`：遍历角色全部已学内功并复用原版事件生成逻辑。
- `AppendEventChain`：保持当前主内功在前，将其他事件链安全追加到尾部。

`BepInExLoader` 继续通过 Harmony 注册补丁。现有 `MartialNumPatch` 和 `ModComponent` 不承担新的战斗职责。

## 数据流

1. 游戏调用 `BattleActor.CreateInternalKungfuEffectEvents(stage)`，原版先为当前主内功生成事件。
2. 后置补丁检查是否处于七个主内功战斗阶段，并确认角色当前服务于我方阵营。
3. 从 `BattleActor.info.characterInstance` 获取角色和全部已学内功。
4. 跳过已经由原版生成的当前主内功实例。
5. 对每本其他内功，仅在同步调用期间临时替换 `activedInternalKungfu` 和 `m_activedInternalKunfuId`。
6. 在递归保护开启时再次调用原版事件生成方法；递归进入的后置补丁立即返回，不再展开。
7. 使用 `try/finally` 恢复角色原有主内功对象与 ID。
8. 将生成的事件链追加到原版结果；若原版结果为空，则第一条成功生成的事件成为返回结果。
9. 游戏原有事件分发器按链顺序处理所有效果。

## 安全与错误处理

- 使用 `[ThreadStatic]` 递归标志，防止补丁调用原方法时无限递归。
- 每次临时替换主内功都必须在 `finally` 中恢复对象和 ID。
- 不调用 `SetActiveInternalKungfu`，避免触发属性重算、UI 刷新或存档状态变化。
- 角色、战斗信息、内功列表或事件为空时直接保留原版结果。
- 单本内功生成失败时记录错误并继续，最外层异常也不得阻断原版当前主内功事件。
- 事件去重只跳过当前主内功实例；同模板或同类效果是否叠加继续交给游戏原有条件与限制决定。

## 测试与验证

- 先新增失败的源码回归检查，要求存在以下行为：
  - 补丁目标为 `BattleActor.CreateInternalKungfuEffectEvents`。
  - 阵营过滤只包含 `Player` 和 `Allie`。
  - 七个主内功战斗阶段均被明确允许。
  - 存在递归保护和 `try/finally` 状态恢复。
  - 不调用 `SetActiveInternalKungfu`。
- 扩展 Harmony 目标解析检查，确认新方法和签名能在当前游戏程序集解析。
- 运行全部现有源码检查和 Release 构建，要求零错误、零警告。
- 将构建出的 DLL 备份后部署到本机游戏 Mod 目录。
- 游戏内验证主角和队友的非主内功效果能触发、敌人仍只触发当前主内功，并检查 `BepInEx/LogOutput.log` 无新增异常。

## 版本与文档

- 插件版本从 `1.0.0` 提升为 `1.1.0`。
- README 增加“我方全部内功主内功战斗效果同时生效”的说明，并明确敌人不受影响、战斗外常驻属性不重复结算。
- 本地构建和部署完成后提交代码；是否推送仓库或创建新 Release 由用户另行决定。
