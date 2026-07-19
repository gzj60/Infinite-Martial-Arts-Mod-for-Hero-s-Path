# Item Spawner Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and locally deploy an independent BepInEx 6 IL2CPP Mod that opens a searchable item-generation window with `F8` and adds `1–999` copies of any loaded game item to the team inventory.

**Architecture:** A small BepInEx entry point owns one injected runtime MonoBehaviour. Managed services load and search the game item table and validate grants; a programmatic TextMeshPro/UGUI window renders a pooled scrolling list, stages generated records in a `GameItemPack`, and hands that pack to `PlayerTeamManager.Instance.PickupPack`.

**Tech Stack:** C# 10, .NET 6, BepInEx 6 IL2CPP, Il2CppInterop, Unity UGUI, TextMeshPro, PowerShell regression and reflection checks.

## Global Constraints

- Create a separate `ItemSpawner.dll`; do not modify or depend on `EnhanceGameplay.dll`.
- Use plugin GUID `com.haxx.ItemSpawner`, display name `ItemSpawner`, author `Haxx`, and version `1.0.0`.
- Bind the toggle key through BepInEx configuration with default `UnityEngine.KeyCode.F8`.
- Show every valid record in the currently loaded `ItemDataScriptObject.ItemData`, including quest, hidden, and loaded DLC items.
- Search only by current-language item name and item ID; use internal name only as the display fallback.
- Accept decimal quantities from `1` through `999`, with default `1`.
- Stage generated items through `GameItemPack.AddItem(itemId, quantity, false)`, then use `PlayerTeamManager.Instance.PickupPack(pack)` for native inventory handling and feedback.
- Do not change `Time.timeScale`, item templates, loot tables, quests, shops, or save formats.
- Do not reference or package UniverseLib.
- Restore the cursor visibility and lock state on every close, destroy, and failure path.
- Build and test against `E:\SteamLibrary\steamapps\common\WulinSH`.
- Do not push the repository or create a GitHub Release.

---

## File Map

- `src/ItemSpawner/ItemSpawner.csproj` — isolated project and game assembly references.
- `src/ItemSpawner/ItemSpawner/ItemEntry.cs` — one searchable managed item record.
- `src/ItemSpawner/ItemSpawner/ItemCatalog.cs` — loads, sorts, and filters the game item table.
- `src/ItemSpawner/ItemSpawner/QuantityParser.cs` — strict `1–999` quantity validation.
- `src/ItemSpawner/ItemSpawner/GrantResult.cs` — result value returned to the UI.
- `src/ItemSpawner/ItemSpawner/ItemGrantService.cs` — validates runtime state and calls the native inventory API.
- `src/ItemSpawner/ItemSpawner/ItemSpawnerPlugin.cs` — BepInEx metadata, configuration, logging, and runtime registration.
- `src/ItemSpawner/ItemSpawner/ItemSpawnerRuntime.cs` — hotkey lifecycle, cursor ownership, and window coordination.
- `src/ItemSpawner/ItemSpawner/UiFactory.cs` — focused UGUI/TextMeshPro object construction helpers.
- `src/ItemSpawner/ItemSpawner/VirtualizedItemList.cs` — pooled list rows and scroll-position binding.
- `src/ItemSpawner/ItemSpawner/ItemSpawnerWindow.cs` — window composition, search, selection, drag, quantity, and status behavior.
- `tests/check-item-spawner-catalog.ps1` — catalog source contract.
- `tests/check-item-spawner-grant.ps1` — quantity and grant source contract.
- `tests/check-item-spawner-ui.ps1` — plugin, hotkey, UGUI, list, and cursor source contract.
- `tests/check-item-spawner-targets.ps1` — live game API and built-plugin metadata compatibility.
- `README.md` — build, install, usage, warning, and verification instructions.

---

### Task 1: Independent Project and Searchable Item Catalog

**Files:**
- Create: `tests/check-item-spawner-catalog.ps1`
- Create: `src/ItemSpawner/ItemSpawner.csproj`
- Create: `src/ItemSpawner/ItemSpawner/ItemEntry.cs`
- Create: `src/ItemSpawner/ItemSpawner/ItemCatalog.cs`

**Interfaces:**
- Consumes: `BaseDataClass.GetGameData<GameData.ItemDataScriptObject>()`, `ItemDataScriptObject.ItemData`, `GameUtil.GetName(ItemData, bool)`, `ItemData.Uid`, and `ItemData.UName`.
- Produces: `ItemEntry(GameData.ItemData, string, string)`, `ItemCatalog.TryLoad(out string)`, `ItemCatalog.Search(string)`, and `ItemCatalog.Loaded`.

- [ ] **Step 1: Write the failing catalog contract check**

Create `tests/check-item-spawner-catalog.ps1` with this complete content:

```powershell
param([string]$SourceRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ItemSpawner\ItemSpawner'
}

$entryPath = Join-Path $SourceRoot 'ItemEntry.cs'
$catalogPath = Join-Path $SourceRoot 'ItemCatalog.cs'
$entry = if (Test-Path -LiteralPath $entryPath) { Get-Content -Raw -LiteralPath $entryPath } else { '' }
$catalog = if (Test-Path -LiteralPath $catalogPath) { Get-Content -Raw -LiteralPath $catalogPath } else { '' }
$failures = @()

function Require-Pattern([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) { $script:failures += $Message }
}

Require-Pattern $entry 'GameData\.ItemData\s+Template' 'ItemEntry must retain the game item template.'
Require-Pattern $entry 'IdText' 'ItemEntry must cache the decimal item ID text.'
Require-Pattern $entry 'Name\.Contains\(query,\s*StringComparison\.OrdinalIgnoreCase\)' 'Name search must be case-insensitive.'
Require-Pattern $entry 'IdText\.Contains\(query,\s*StringComparison\.Ordinal\)' 'ID search must use decimal text.'
Require-Pattern $catalog 'BaseDataClass\.GetGameData<ItemDataScriptObject>\(\)' 'Catalog must use the game data table API.'
Require-Pattern $catalog '\.ItemData' 'Catalog must enumerate ItemDataScriptObject.ItemData.'
Require-Pattern $catalog 'GameUtil\.GetName\(item,\s*false\)' 'Catalog must use the current-language item name.'
Require-Pattern $catalog 'item\.UName' 'Catalog must use UName only as the fallback.'
Require-Pattern $catalog 'entries\.Sort' 'Catalog must sort results.'
Require-Pattern $catalog 'left\.Id\.CompareTo\(right\.Id\)' 'Catalog must sort by ascending item ID.'
Require-Pattern $catalog 'public\s+List<ItemEntry>\s+Search\(string\s+query\)' 'Catalog must expose managed search results.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) item catalog check(s) failed."
}

Write-Host 'Item spawner catalog checks passed.'
```

- [ ] **Step 2: Run the catalog check and verify RED**

Run:

```powershell
.\tests\check-item-spawner-catalog.ps1
```

Expected: the script throws with missing `ItemEntry` and `ItemCatalog` requirements.

- [ ] **Step 3: Create the independent project**

Create `src/ItemSpawner/ItemSpawner.csproj` with the same local-reference style as `EnhanceGameplay.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>ItemSpawner</AssemblyName>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="BepInEx.Core"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\core\BepInEx.Core.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="BepInEx.Unity.IL2CPP"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\core\BepInEx.Unity.IL2CPP.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Il2CppInterop.Runtime"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\core\Il2CppInterop.Runtime.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Il2Cppmscorlib"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\Il2Cppmscorlib.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\Assembly-CSharp.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="ModShare.Runtime"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\ModShare.Runtime.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="GamePlugins.InspectorEnhance.Runtime"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\GamePlugins.InspectorEnhance.Runtime.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\UnityEngine.CoreModule.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.InputLegacyModule"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\UnityEngine.InputLegacyModule.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.UI"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\UnityEngine.UI.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.UIModule"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\UnityEngine.UIModule.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Unity.TextMeshPro"><HintPath>E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\interop\Unity.TextMeshPro.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Implement the item record**

Create `src/ItemSpawner/ItemSpawner/ItemEntry.cs`:

```csharp
using System;
using GameData;

namespace ItemSpawner;

public sealed class ItemEntry
{
    public ItemData Template { get; }
    public int Id { get; }
    public string IdText { get; }
    public string Name { get; }
    public string InternalName { get; }

    public ItemEntry(ItemData template, string name, string internalName)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Id = template.Uid;
        IdText = Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Name = name;
        InternalName = internalName;
    }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        query = query.Trim();
        return Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            IdText.Contains(query, StringComparison.Ordinal);
    }
}
```

- [ ] **Step 5: Implement retryable catalog loading and search**

Create `src/ItemSpawner/ItemSpawner/ItemCatalog.cs`:

```csharp
using System;
using System.Collections.Generic;
using GameData;
using WuLin;

namespace ItemSpawner;

public sealed class ItemCatalog
{
    private readonly List<ItemEntry> entries = new();

    public bool Loaded { get; private set; }
    public IReadOnlyList<ItemEntry> Entries => entries;

    public bool TryLoad(out string error)
    {
        error = string.Empty;
        if (Loaded)
        {
            return true;
        }

        ItemDataScriptObject table;
        try
        {
            table = BaseDataClass.GetGameData<ItemDataScriptObject>();
        }
        catch (Exception)
        {
            error = "读取游戏物品数据失败。";
            return false;
        }
        if (table == null || table.ItemData == null || table.ItemData.Length == 0)
        {
            error = "游戏物品数据尚未就绪。";
            return false;
        }

        entries.Clear();
        for (int i = 0; i < table.ItemData.Length; i++)
        {
            ItemData item = table.ItemData[i];
            if (item == null || item.Uid <= 0)
            {
                continue;
            }

            string internalName = item.UName ?? string.Empty;
            string displayName;
            try
            {
                displayName = GameUtil.GetName(item, false);
            }
            catch (Exception)
            {
                displayName = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = string.IsNullOrWhiteSpace(internalName) ? "未命名物品" : internalName;
            }

            entries.Add(new ItemEntry(item, displayName, internalName));
        }

        entries.Sort((left, right) => left.Id.CompareTo(right.Id));
        Loaded = entries.Count > 0;
        if (!Loaded)
        {
            error = "物品表中没有有效记录。";
        }
        return Loaded;
    }

    public List<ItemEntry> Search(string query)
    {
        List<ItemEntry> results = new();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Matches(query))
            {
                results.Add(entries[i]);
            }
        }
        return results;
    }
}
```

- [ ] **Step 6: Run the focused check and Release build**

Run:

```powershell
.\tests\check-item-spawner-catalog.ps1
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
```

Expected: `Item spawner catalog checks passed.` and build succeeds with zero warnings and zero errors.

- [ ] **Step 7: Commit the catalog slice**

```powershell
git add -- tests/check-item-spawner-catalog.ps1 src/ItemSpawner
git commit -m "Add searchable item catalog"
```

---

### Task 2: Quantity Validation and Native Inventory Grant Service

**Files:**
- Create: `tests/check-item-spawner-grant.ps1`
- Create: `src/ItemSpawner/ItemSpawner/QuantityParser.cs`
- Create: `src/ItemSpawner/ItemSpawner/GrantResult.cs`
- Create: `src/ItemSpawner/ItemSpawner/ItemGrantService.cs`

**Interfaces:**
- Consumes: `ItemEntry.Id`, `PlayerTeamManager.Instance.TeamInventory`, `GameItemPack.AddItem(int, int, bool)`, and `PlayerTeamManager.PickupPack`.
- Produces: `QuantityParser.TryParse(string, out int, out string)`, `ItemGrantService.IsReady(out string)`, and `ItemGrantService.Grant(ItemEntry, int)`.

- [ ] **Step 1: Write the failing grant contract check**

Create `tests/check-item-spawner-grant.ps1`:

```powershell
param([string]$SourceRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ItemSpawner\ItemSpawner'
}

$quantityPath = Join-Path $SourceRoot 'QuantityParser.cs'
$grantPath = Join-Path $SourceRoot 'ItemGrantService.cs'
$quantity = if (Test-Path -LiteralPath $quantityPath) { Get-Content -Raw -LiteralPath $quantityPath } else { '' }
$grant = if (Test-Path -LiteralPath $grantPath) { Get-Content -Raw -LiteralPath $grantPath } else { '' }
$failures = @()

function Require-Pattern([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) { $script:failures += $Message }
}
function Forbid-Pattern([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -match $Pattern) { $script:failures += $Message }
}

Require-Pattern $quantity 'int\.TryParse' 'Quantity must be parsed as an integer.'
Require-Pattern $quantity 'quantity\s*<\s*1\s*\|\|\s*quantity\s*>\s*999' 'Quantity must be limited to 1 through 999.'
Require-Pattern $grant 'PlayerTeamManager\.Instance' 'Grant service must obtain the player team manager.'
Require-Pattern $grant '\.TeamInventory' 'Grant service must target the team inventory.'
Require-Pattern $grant 'GameItemPack\s+pack\s*=\s*new\(\)' 'Grant service must stage generated items in a native item pack.'
Require-Pattern $grant 'pack\.AddItem\(entry\.Id,\s*quantity,\s*false\)' 'Grant service must bypass acquisition checks for generated quest items.'
Require-Pattern $grant 'PlayerTeamManager\.Instance\.PickupPack\(pack\)' 'Grant service must use the native pickup flow and feedback.'
Forbid-Pattern $grant 'TeamInventory\.AddItem' 'Grant service must not directly add generated items with inventory acquisition checks.'
Require-Pattern $grant 'catch\s*\(Exception\s+ex\)' 'Grant failures must be converted into a result.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) item grant check(s) failed."
}

Write-Host 'Item spawner grant checks passed.'
```

- [ ] **Step 2: Run the grant check and verify RED**

Run `.\tests\check-item-spawner-grant.ps1`.

Expected: failure for the missing quantity and grant service files.

- [ ] **Step 3: Implement strict quantity parsing**

Create `src/ItemSpawner/ItemSpawner/QuantityParser.cs`:

```csharp
using System.Globalization;

namespace ItemSpawner;

public static class QuantityParser
{
    public static bool TryParse(string raw, out int quantity, out string error)
    {
        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out quantity))
        {
            error = "数量必须是整数。";
            return false;
        }
        if (quantity < 1 || quantity > 999)
        {
            error = "数量范围为 1–999。";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
```

- [ ] **Step 4: Implement the grant result and service**

Create `src/ItemSpawner/ItemSpawner/GrantResult.cs`:

```csharp
namespace ItemSpawner;

public readonly struct GrantResult
{
    public bool Success { get; }
    public string Message { get; }

    public GrantResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}
```

Create `src/ItemSpawner/ItemSpawner/ItemGrantService.cs`:

```csharp
using System;
using WuLin;

namespace ItemSpawner;

public sealed class ItemGrantService
{
    public bool IsReady(out string reason)
    {
        PlayerTeamManager manager = PlayerTeamManager.Instance;
        if (manager == null || manager.TeamInventory == null)
        {
            reason = "请先进入一个有效存档。";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    public GrantResult Grant(ItemEntry entry, int quantity)
    {
        if (entry == null)
        {
            return new GrantResult(false, "请先选择物品。");
        }
        if (quantity < 1 || quantity > 999)
        {
            return new GrantResult(false, "数量范围为 1–999。");
        }
        if (!IsReady(out string reason))
        {
            return new GrantResult(false, reason);
        }

        try
        {
            GameItemPack pack = new();
            bool added = pack.AddItem(entry.Id, quantity, false);
            if (!added)
            {
                return new GrantResult(false, "生成失败，物品数据无效。");
            }
            PlayerTeamManager.Instance.PickupPack(pack);
            return new GrantResult(true, $"已获得“{entry.Name}”×{quantity}。");
        }
        catch (Exception ex)
        {
            ItemSpawnerPlugin.Logger?.LogError($"Failed to grant item {entry.Id} x{quantity}: {ex}");
            return new GrantResult(false, "生成物品时发生错误，请查看 BepInEx 日志。");
        }
    }
}
```

Because `ItemGrantService` references the plugin logger before the plugin file exists, add this temporary build-safe logger bridge at the end of `ItemGrantService.cs` in this task only:

```csharp
internal static class ItemSpawnerPlugin
{
    internal static BepInEx.Logging.ManualLogSource Logger { get; set; }
}
```

Task 3 replaces this bridge with the real BepInEx plugin class before its build.

- [ ] **Step 5: Run the grant check and build**

```powershell
.\tests\check-item-spawner-grant.ps1
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
```

Expected: grant checks pass and build has zero warnings/errors.

- [ ] **Step 6: Commit the grant slice**

```powershell
git add -- tests/check-item-spawner-grant.ps1 src/ItemSpawner/ItemSpawner
git commit -m "Add native item grant service"
```

---

### Task 3: BepInEx Entry Point, Hotkey Lifecycle, and Cursor-Safe Window Shell

**Files:**
- Create: `tests/check-item-spawner-ui.ps1`
- Create: `src/ItemSpawner/ItemSpawner/ItemSpawnerPlugin.cs`
- Create: `src/ItemSpawner/ItemSpawner/ItemSpawnerRuntime.cs`
- Create: `src/ItemSpawner/ItemSpawner/UiFactory.cs`
- Create: `src/ItemSpawner/ItemSpawner/ItemSpawnerWindow.cs`
- Modify: `src/ItemSpawner/ItemSpawner/ItemGrantService.cs`

**Interfaces:**
- Consumes: `BasePlugin.AddComponent<ItemSpawnerRuntime>()`, `ConfigEntry<KeyCode>`, Unity input, cursor APIs, and TextMeshPro/UGUI.
- Produces: `ItemSpawnerRuntime.ToggleWindow()`, `ItemSpawnerWindow.Show()`, `Hide()`, `Tick()`, and `Dispose()`.

- [ ] **Step 1: Write the failing UI shell contract check**

Create `tests/check-item-spawner-ui.ps1`:

```powershell
param([string]$SourceRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ItemSpawner\ItemSpawner'
}

$files = Get-ChildItem -LiteralPath $SourceRoot -File -Filter '*.cs' -ErrorAction SilentlyContinue
$source = ($files | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
$failures = @()
function Require-Pattern([string]$Pattern, [string]$Message) {
    if ($source -notmatch $Pattern) { $script:failures += $Message }
}
function Forbid-Pattern([string]$Pattern, [string]$Message) {
    if ($source -match $Pattern) { $script:failures += $Message }
}

Require-Pattern 'BepInPlugin\("com\.haxx\.ItemSpawner",\s*"ItemSpawner",\s*"1\.0\.0"\)' 'Plugin metadata must be independent and versioned.'
Require-Pattern 'Config\.Bind\([^;]+KeyCode\.F8' 'Toggle key must default to F8 through BepInEx config.'
Require-Pattern 'AddComponent<ItemSpawnerRuntime>\(\)' 'Plugin must add its own injected runtime.'
Require-Pattern 'Input\.GetKeyDown\(ItemSpawnerPlugin\.ToggleKey\.Value\)' 'Runtime must use the configured key.'
Require-Pattern 'Cursor\.visible' 'Runtime must save and restore cursor visibility.'
Require-Pattern 'Cursor\.lockState' 'Runtime must save and restore cursor lock state.'
Require-Pattern 'CanvasScaler\.ScaleMode\.ScaleWithScreenSize' 'Window must scale with resolution.'
Require-Pattern 'TextMeshProUGUI' 'Window must use TextMeshPro text.'
Require-Pattern 'TMP_InputField' 'Window must use TextMeshPro input.'
Require-Pattern 'GraphicRaycaster' 'Window must receive UGUI pointer events.'
Forbid-Pattern 'Time\.timeScale' 'Window must not pause or accelerate the game.'
Forbid-Pattern 'UniverseLib' 'Standalone plugin must not depend on UniverseLib.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) item spawner UI check(s) failed."
}
Write-Host 'Item spawner UI checks passed.'
```

- [ ] **Step 2: Run the UI check and verify RED**

Run `.\tests\check-item-spawner-ui.ps1`.

Expected: failure for missing plugin metadata, hotkey, cursor, and UGUI behavior.

- [ ] **Step 3: Replace the logger bridge with the real plugin**

Delete only the temporary `internal static class ItemSpawnerPlugin` block from `ItemGrantService.cs`, then create `src/ItemSpawner/ItemSpawner/ItemSpawnerPlugin.cs`:

```csharp
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ItemSpawner;

[BepInPlugin("com.haxx.ItemSpawner", "ItemSpawner", "1.0.0")]
public sealed class ItemSpawnerPlugin : BasePlugin
{
    private ItemSpawnerRuntime runtime;
    internal static ConfigEntry<KeyCode> ToggleKey { get; private set; }
    internal static ManualLogSource Logger { get; private set; }

    public override void Load()
    {
        Logger = base.Log;
        System.AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        ToggleKey = Config.Bind("General", "ToggleKey", KeyCode.F8, "打开或关闭物品生成器窗口。");
        runtime = AddComponent<ItemSpawnerRuntime>();
        Logger.LogMessage("ItemSpawner 1.0.0 loaded. Press F8 to toggle the window.");
    }

    public override bool Unload()
    {
        System.AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        if (runtime != null)
        {
            UnityEngine.Object.Destroy(runtime);
            runtime = null;
        }
        return true;
    }

    private static void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs args)
    {
        Logger?.LogError($"Unhandled ItemSpawner exception: {args.ExceptionObject}");
    }
}
```

Now replace the three silent catalog paths in `ItemCatalog.TryLoad` with logged variants:

```csharp
catch (Exception ex)
{
    ItemSpawnerPlugin.Logger?.LogError($"Failed to read the item table: {ex}");
    error = "读取游戏物品数据失败。";
    return false;
}
```

```csharp
if (item == null || item.Uid <= 0)
{
    ItemSpawnerPlugin.Logger?.LogDebug($"Skipping invalid item table record at index {i}.");
    continue;
}
```

```csharp
catch (Exception ex)
{
    ItemSpawnerPlugin.Logger?.LogWarning($"Failed to localize item {item.Uid}: {ex.Message}");
    displayName = string.Empty;
}
```

- [ ] **Step 4: Implement UI construction helpers**

Create `src/ItemSpawner/ItemSpawner/UiFactory.cs` with helpers that always create UI objects with a `RectTransform` and the game font:

```csharp
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ItemSpawner;

internal static class UiFactory
{
    internal static TMP_FontAsset FindGameFont()
    {
        TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
        foreach (TextMeshProUGUI text in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
        {
            if (text == null || text.font == null)
            {
                continue;
            }
            fallback ??= text.font;
            string value = text.text;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] > 127)
                    {
                        return text.font;
                    }
                }
            }
        }
        return fallback;
    }

    internal static RectTransform Rect(string name, Transform parent)
    {
        GameObject gameObject = new(name, Il2CppType.Of<RectTransform>());
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    internal static TextMeshProUGUI Text(RectTransform parent, TMP_FontAsset font, string value, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = Rect("Text", parent);
        TextMeshProUGUI text = ((Component)rect).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 4f);
        rect.offsetMax = new Vector2(-10f, -4f);
        return text;
    }

    internal static Button Button(RectTransform parent, TMP_FontAsset font, string label)
    {
        Image image = ((Component)parent).gameObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.25f, 0.29f, 1f);
        Button button = ((Component)parent).gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text(parent, font, label, 24f, TextAlignmentOptions.Center);
        return button;
    }

    internal static TMP_InputField Input(RectTransform parent, TMP_FontAsset font, string placeholderText)
    {
        Image image = ((Component)parent).gameObject.AddComponent<Image>();
        image.color = new Color(0.10f, 0.12f, 0.15f, 1f);
        RectTransform viewport = Rect("Viewport", parent);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 4f);
        viewport.offsetMax = new Vector2(-12f, -4f);
        TextMeshProUGUI value = Text(viewport, font, string.Empty, 24f, TextAlignmentOptions.MidlineLeft);
        TextMeshProUGUI placeholder = Text(viewport, font, placeholderText, 24f, TextAlignmentOptions.MidlineLeft);
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);
        TMP_InputField input = ((Component)parent).gameObject.AddComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }
}
```

- [ ] **Step 5: Implement the cursor-safe runtime**

Create `src/ItemSpawner/ItemSpawner/ItemSpawnerRuntime.cs`:

```csharp
using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ItemSpawner;

public sealed class ItemSpawnerRuntime : MonoBehaviour
{
    private ItemSpawnerWindow window;
    private bool cursorCaptured;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public ItemSpawnerRuntime(IntPtr pointer) : base(pointer) { }

    public void Awake()
    {
        window = new ItemSpawnerWindow(new ItemCatalog(), new ItemGrantService());
    }

    public void Update()
    {
        if (Input.GetKeyDown(ItemSpawnerPlugin.ToggleKey.Value))
        {
            ToggleWindow();
        }
        if (window.IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseWindow();
        }
        if (window.IsVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            window.Tick();
        }
    }

    internal void ToggleWindow()
    {
        if (window.IsVisible) CloseWindow(); else OpenWindow();
    }

    private void OpenWindow()
    {
        if (!cursorCaptured)
        {
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            cursorCaptured = true;
        }
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        try
        {
            window.Show(CloseWindow);
        }
        catch (Exception ex)
        {
            ItemSpawnerPlugin.Logger.LogError($"Failed to open ItemSpawner: {ex}");
            RestoreCursor();
        }
    }

    private void CloseWindow()
    {
        window.Hide();
        RestoreCursor();
    }

    private void RestoreCursor()
    {
        if (!cursorCaptured) return;
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLock;
        cursorCaptured = false;
    }

    public void OnDestroy()
    {
        window?.Dispose();
        RestoreCursor();
    }
}
```

- [ ] **Step 6: Build the UGUI window shell**

Create `src/ItemSpawner/ItemSpawner/ItemSpawnerWindow.cs` with this complete shell implementation:

```csharp
using System;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ItemSpawner;

internal sealed class ItemSpawnerWindow
{
    private const float WindowWidth = 900f;
    private const float WindowHeight = 650f;
    private const float TitleHeight = 54f;
    private readonly ItemCatalog catalog;
    private readonly ItemGrantService grantService;
    private GameObject root;
    private GameObject ownedEventSystem;
    private RectTransform canvasRect;
    private RectTransform windowRect;
    private RectTransform titleRect;
    private RectTransform closeRect;
    private Action closeAction;
    private bool dragging;
    private Vector2 lastPointer;

    internal bool IsVisible { get; private set; }
    internal TMP_InputField SearchInput { get; private set; }
    internal TMP_InputField QuantityInput { get; private set; }
    internal ScrollRect ItemScroll { get; private set; }
    internal RectTransform ItemContent { get; private set; }
    internal Button GenerateButton { get; private set; }
    internal TextMeshProUGUI StatusText { get; private set; }

    internal ItemSpawnerWindow(ItemCatalog catalog, ItemGrantService grantService)
    {
        this.catalog = catalog;
        this.grantService = grantService;
    }

    internal void Show(Action closeAction)
    {
        this.closeAction = closeAction;
        if (root == null)
        {
            try
            {
                Build();
            }
            catch
            {
                Dispose();
                throw;
            }
        }
        root.SetActive(true);
        IsVisible = true;
        if (catalog.TryLoad(out string error))
        {
            StatusText.text = $"已加载 {catalog.Entries.Count} 件物品。";
        }
        else
        {
            StatusText.text = error;
        }
        GenerateButton.interactable = catalog.Loaded && grantService.IsReady(out _);
    }

    internal void Hide()
    {
        if (SearchInput != null) SearchInput.DeactivateInputField(false);
        if (QuantityInput != null) QuantityInput.DeactivateInputField(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (root != null) root.SetActive(false);
        IsVisible = false;
        dragging = false;
    }

    internal void Tick()
    {
        if (!IsVisible || root == null) return;
        HandleDragging();
    }

    internal void Dispose()
    {
        Hide();
        if (root != null) Object.Destroy(root);
        if (ownedEventSystem != null) Object.Destroy(ownedEventSystem);
        root = null;
        ownedEventSystem = null;
    }

    private void Build()
    {
        EnsureEventSystem();
        TMP_FontAsset font = UiFactory.FindGameFont();
        if (font == null) throw new InvalidOperationException("Could not find a TextMeshPro font.");

        root = new GameObject("ItemSpawnerCanvas", Il2CppType.Of<RectTransform>());
        Object.DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        canvasRect = root.GetComponent<RectTransform>();

        windowRect = UiFactory.Rect("Window", root.transform);
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);
        windowRect.anchoredPosition = Vector2.zero;
        Image background = ((Component)windowRect).gameObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.065f, 0.08f, 0.98f);

        titleRect = UiFactory.Rect("TitleBar", windowRect);
        SetStretchTop(titleRect, TitleHeight, 0f, 0f);
        Image titleBackground = ((Component)titleRect).gameObject.AddComponent<Image>();
        titleBackground.color = new Color(0.12f, 0.15f, 0.19f, 1f);
        UiFactory.Text(titleRect, font, "物品生成器", 28f, TextAlignmentOptions.Center);

        closeRect = UiFactory.Rect("Close", titleRect);
        closeRect.anchorMin = new Vector2(1f, 0f);
        closeRect.anchorMax = Vector2.one;
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.sizeDelta = new Vector2(54f, 0f);
        closeRect.anchoredPosition = Vector2.zero;
        Button closeButton = UiFactory.Button(closeRect, font, "×");
        closeButton.onClick.AddListener((UnityAction)(() => this.closeAction?.Invoke()));

        RectTransform searchRect = UiFactory.Rect("Search", windowRect);
        SetStretchTop(searchRect, 48f, 68f, 20f);
        SearchInput = UiFactory.Input(searchRect, font, "搜索名称或 ID");

        RectTransform viewport = UiFactory.Rect("ItemViewport", windowRect);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(20f, 132f);
        viewport.offsetMax = new Vector2(-20f, -126f);
        ((Component)viewport).gameObject.AddComponent<RectMask2D>();
        ItemScroll = ((Component)viewport).gameObject.AddComponent<ScrollRect>();
        ItemScroll.viewport = viewport;
        ItemScroll.horizontal = false;
        ItemScroll.vertical = true;
        ItemScroll.movementType = ScrollRect.MovementType.Clamped;
        ItemScroll.scrollSensitivity = 34f;
        ItemContent = UiFactory.Rect("Content", viewport);
        ItemContent.anchorMin = new Vector2(0f, 1f);
        ItemContent.anchorMax = new Vector2(1f, 1f);
        ItemContent.pivot = new Vector2(0.5f, 1f);
        ItemContent.sizeDelta = new Vector2(0f, 1f);
        ItemScroll.content = ItemContent;

        RectTransform quantityRect = UiFactory.Rect("Quantity", windowRect);
        quantityRect.anchorMin = new Vector2(0f, 0f);
        quantityRect.anchorMax = new Vector2(0f, 0f);
        quantityRect.pivot = Vector2.zero;
        quantityRect.anchoredPosition = new Vector2(20f, 70f);
        quantityRect.sizeDelta = new Vector2(180f, 44f);
        QuantityInput = UiFactory.Input(quantityRect, font, "数量 1–999");
        QuantityInput.text = "1";

        RectTransform generateRect = UiFactory.Rect("Generate", windowRect);
        generateRect.anchorMin = new Vector2(0f, 0f);
        generateRect.anchorMax = new Vector2(0f, 0f);
        generateRect.pivot = Vector2.zero;
        generateRect.anchoredPosition = new Vector2(216f, 70f);
        generateRect.sizeDelta = new Vector2(170f, 44f);
        GenerateButton = UiFactory.Button(generateRect, font, "生成物品");
        GenerateButton.interactable = false;

        RectTransform statusRect = UiFactory.Rect("Status", windowRect);
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = Vector2.zero;
        statusRect.anchoredPosition = new Vector2(20f, 32f);
        statusRect.sizeDelta = new Vector2(-40f, 32f);
        StatusText = UiFactory.Text(statusRect, font, string.Empty, 20f, TextAlignmentOptions.MidlineLeft);

        RectTransform warningRect = UiFactory.Rect("Warning", windowRect);
        warningRect.anchorMin = new Vector2(0f, 0f);
        warningRect.anchorMax = new Vector2(1f, 0f);
        warningRect.pivot = Vector2.zero;
        warningRect.anchoredPosition = new Vector2(20f, 4f);
        warningRect.sizeDelta = new Vector2(-40f, 28f);
        TextMeshProUGUI warning = UiFactory.Text(warningRect, font, "提示：任务或隐藏物品可能影响存档。", 18f, TextAlignmentOptions.MidlineLeft);
        warning.color = new Color(1f, 0.74f, 0.34f, 1f);
        root.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        ownedEventSystem = new GameObject("ItemSpawnerEventSystem");
        Object.DontDestroyOnLoad(ownedEventSystem);
        ownedEventSystem.AddComponent<EventSystem>();
        ownedEventSystem.AddComponent<StandaloneInputModule>();
    }

    private void HandleDragging()
    {
        Vector2 screenPoint = Input.mousePosition;
        if (Input.GetMouseButtonDown(0) &&
            RectTransformUtility.RectangleContainsScreenPoint(titleRect, screenPoint, null) &&
            !RectTransformUtility.RectangleContainsScreenPoint(closeRect, screenPoint, null) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out lastPointer))
        {
            dragging = true;
        }
        if (dragging && Input.GetMouseButton(0) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 current))
        {
            windowRect.anchoredPosition += current - lastPointer;
            lastPointer = current;
            ClampWindow();
        }
        if (Input.GetMouseButtonUp(0)) dragging = false;
    }

    private void ClampWindow()
    {
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        Vector2 position = windowRect.anchoredPosition;
        position.x = Mathf.Clamp(position.x,
            -canvasWidth * 0.5f - WindowWidth * 0.5f + 120f,
            canvasWidth * 0.5f + WindowWidth * 0.5f - 120f);
        position.y = Mathf.Clamp(position.y,
            -canvasHeight * 0.5f - WindowHeight * 0.5f + TitleHeight,
            canvasHeight * 0.5f + WindowHeight * 0.5f - TitleHeight);
        windowRect.anchoredPosition = position;
    }

    private static void SetStretchTop(RectTransform rect, float height, float top, float horizontalInset)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(horizontalInset, -top - height);
        rect.offsetMax = new Vector2(-horizontalInset, -top);
    }
}
```

- [ ] **Step 7: Run shell checks and build**

```powershell
.\tests\check-item-spawner-ui.ps1
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
```

Expected: UI checks pass; build has zero warnings/errors.

- [ ] **Step 8: Commit the runnable shell**

```powershell
git add -- tests/check-item-spawner-ui.ps1 src/ItemSpawner/ItemSpawner
git commit -m "Add item spawner window shell"
```

---

### Task 4: Live Search and Virtualized Item Rows

**Files:**
- Create: `src/ItemSpawner/ItemSpawner/VirtualizedItemList.cs`
- Modify: `src/ItemSpawner/ItemSpawner/ItemSpawnerWindow.cs`
- Modify: `tests/check-item-spawner-ui.ps1`

**Interfaces:**
- Consumes: `ItemCatalog.TryLoad`, `ItemCatalog.Search`, `ScrollRect`, and `ItemEntry`.
- Produces: `VirtualizedItemList.SetItems(List<ItemEntry>)`, `RefreshVisible()`, `Selected`, and `SelectionChanged`.

- [ ] **Step 1: Extend the UI check and verify RED**

Add these exact requirements before the failure block in `tests/check-item-spawner-ui.ps1`:

```powershell
Require-Pattern 'class\s+VirtualizedItemList' 'UI must pool item rows.'
Require-Pattern 'const\s+int\s+PoolSize\s*=\s*16' 'UI must use a bounded 16-row pool.'
Require-Pattern 'Mathf\.FloorToInt\([^\)]*anchoredPosition\.y\s*/\s*RowHeight' 'Visible binding must follow scroll position.'
Require-Pattern 'SetItems\(List<ItemEntry>\s+items\)' 'Virtualized list must accept filtered results.'
Require-Pattern 'SearchInput\.onValueChanged' 'Search must refresh while typing.'
Require-Pattern 'catalog\.Search\(SearchInput\.text\)' 'Search must filter the loaded catalog.'
```

Run `.\tests\check-item-spawner-ui.ps1` and expect failures for the missing virtualized list and live search.

- [ ] **Step 2: Implement the pooled list**

Create `src/ItemSpawner/ItemSpawner/VirtualizedItemList.cs` with:

```csharp
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ItemSpawner;

internal sealed class VirtualizedItemList
{
    private const int PoolSize = 16;
    private const float RowHeight = 42f;
    private readonly ScrollRect scroll;
    private readonly RectTransform content;
    private readonly List<Row> rows = new();
    private List<ItemEntry> items = new();
    private int lastFirstIndex = -1;

    internal ItemEntry Selected { get; private set; }
    internal event Action<ItemEntry> SelectionChanged;

    internal VirtualizedItemList(ScrollRect scroll, RectTransform content, TMP_FontAsset font)
    {
        this.scroll = scroll;
        this.content = content;
        for (int i = 0; i < PoolSize; i++)
        {
            RectTransform rect = UiFactory.Rect($"ItemRow{i}", content);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, RowHeight - 2f);
            Image image = ((Component)rect).gameObject.AddComponent<Image>();
            Button button = ((Component)rect).gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI label = UiFactory.Text(rect, font, string.Empty, 22f, TextAlignmentOptions.MidlineLeft);
            Row row = new(rect, image, button, label);
            button.onClick.AddListener((UnityAction)(() => Select(row)));
            rows.Add(row);
        }
    }

    internal void SetItems(List<ItemEntry> items)
    {
        this.items = items ?? new List<ItemEntry>();
        if (Selected != null && !this.items.Contains(Selected))
        {
            Selected = null;
            SelectionChanged?.Invoke(null);
        }
        float viewportHeight = scroll.viewport == null ? 0f : scroll.viewport.rect.height;
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(viewportHeight, this.items.Count * RowHeight));
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        scroll.verticalNormalizedPosition = 1f;
        lastFirstIndex = -1;
        RefreshVisible();
    }

    internal void RefreshVisible()
    {
        int maxFirst = Mathf.Max(0, items.Count - PoolSize);
        int first = Mathf.Clamp(Mathf.FloorToInt(content.anchoredPosition.y / RowHeight), 0, maxFirst);
        if (first == lastFirstIndex) return;
        lastFirstIndex = first;
        for (int i = 0; i < rows.Count; i++)
        {
            int itemIndex = first + i;
            Row row = rows[i];
            bool active = itemIndex < items.Count;
            ((Component)row.Rect).gameObject.SetActive(active);
            if (!active)
            {
                row.Entry = null;
                continue;
            }
            row.Entry = items[itemIndex];
            row.Rect.anchoredPosition = new Vector2(0f, -itemIndex * RowHeight);
            row.Label.text = $"{row.Entry.IdText}    {row.Entry.Name}";
            row.Image.color = row.Entry == Selected
                ? new Color(0.25f, 0.50f, 0.72f, 0.95f)
                : new Color(0.13f, 0.15f, 0.18f, i % 2 == 0 ? 0.95f : 0.82f);
        }
    }

    private void Select(Row row)
    {
        if (row.Entry == null) return;
        Selected = row.Entry;
        lastFirstIndex = -1;
        RefreshVisible();
        SelectionChanged?.Invoke(Selected);
    }

    private sealed class Row
    {
        internal RectTransform Rect { get; }
        internal Image Image { get; }
        internal Button Button { get; }
        internal TextMeshProUGUI Label { get; }
        internal ItemEntry Entry { get; set; }

        internal Row(RectTransform rect, Image image, Button button, TextMeshProUGUI label)
        {
            Rect = rect;
            Image = image;
            Button = button;
            Label = label;
        }
    }
}
```

- [ ] **Step 3: Wire retryable loading and live search into the window**

Add these fields to `ItemSpawnerWindow`:

```csharp
private VirtualizedItemList list;
private float nextCatalogRetry;
```

Immediately after assigning `ItemScroll.content = ItemContent` in `Build`, create the pool and register search/selection listeners:

```csharp
list = new VirtualizedItemList(ItemScroll, ItemContent, font);
SearchInput.onValueChanged.AddListener((UnityAction<string>)(_ => RefreshSearch()));
list.SelectionChanged += _ => RefreshGenerateState();
```

Add these complete methods:

```csharp
private void RefreshSearch()
{
    if (!catalog.TryLoad(out string error))
    {
        StatusText.text = error;
        list.SetItems(new List<ItemEntry>());
        return;
    }
    List<ItemEntry> results = catalog.Search(SearchInput.text);
    list.SetItems(results);
    StatusText.text = $"找到 {results.Count} 件物品。";
    RefreshGenerateState();
}

private void RefreshGenerateState()
{
    bool quantityValid = QuantityParser.TryParse(QuantityInput.text, out _, out _);
    GenerateButton.interactable = list.Selected != null && quantityValid && grantService.IsReady(out _);
}
```

Replace the catalog/status block in `Show` with:

```csharp
RefreshSearch();
```

Replace `Tick` with this complete method while retaining `HandleDragging`:

```csharp
internal void Tick()
{
    if (!IsVisible || root == null) return;
    HandleDragging();
    list.RefreshVisible();
    if (!catalog.Loaded && Time.unscaledTime >= nextCatalogRetry)
    {
        nextCatalogRetry = Time.unscaledTime + 1f;
        RefreshSearch();
    }
}
```

- [ ] **Step 4: Run the UI and catalog checks, then build**

```powershell
.\tests\check-item-spawner-catalog.ps1
.\tests\check-item-spawner-ui.ps1
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
```

Expected: both checks pass; build has zero warnings/errors.

- [ ] **Step 5: Commit searchable list behavior**

```powershell
git add -- tests/check-item-spawner-ui.ps1 src/ItemSpawner/ItemSpawner
git commit -m "Add searchable virtualized item list"
```

---

### Task 5: Complete Generation Interaction, Compatibility Check, and Documentation

**Files:**
- Modify: `src/ItemSpawner/ItemSpawner/ItemSpawnerWindow.cs`
- Create: `tests/check-item-spawner-targets.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes: `QuantityParser.TryParse`, `VirtualizedItemList.Selected`, `ItemGrantService.Grant`, and the built `ItemSpawner.dll`.
- Produces: the complete user interaction and a reflection-based compatibility report for every required game API.

- [ ] **Step 1: Add the failing generation interaction requirements**

Add to `tests/check-item-spawner-ui.ps1`:

```powershell
Require-Pattern 'QuantityInput\.text\s*=\s*"1"' 'Quantity must default to 1.'
Require-Pattern 'QuantityInput\.characterLimit\s*=\s*3' 'Quantity input must be limited to three characters.'
Require-Pattern 'TMP_InputField\.ContentType\.IntegerNumber' 'Quantity input must request integer input.'
Require-Pattern 'GenerateButton\.onClick' 'Generate button must have a click handler.'
Require-Pattern 'grantService\.Grant\(list\.Selected,\s*quantity\)' 'Click must grant the selected item and validated quantity.'
Require-Pattern '任务或隐藏物品可能影响存档' 'Window must show the confirmed save-risk warning.'
```

Run `.\tests\check-item-spawner-ui.ps1` and expect the new requirements to fail.

- [ ] **Step 2: Complete quantity and generation behavior**

After creating `QuantityInput`, configure it exactly once:

```csharp
QuantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
QuantityInput.characterLimit = 3;
QuantityInput.text = "1";
QuantityInput.onValueChanged.AddListener((UnityAction<string>)(_ => RefreshGenerateState()));
GenerateButton.onClick.AddListener((UnityAction)GenerateSelectedItem);
```

Add the complete click method:

```csharp
private void GenerateSelectedItem()
{
    if (!QuantityParser.TryParse(QuantityInput.text, out int quantity, out string error))
    {
        StatusText.text = error;
        GenerateButton.interactable = false;
        return;
    }
    GrantResult result = grantService.Grant(list.Selected, quantity);
    StatusText.text = result.Message;
    RefreshGenerateState();
}
```

Add a persistent warning TextMeshPro element whose exact text is `提示：任务或隐藏物品可能影响存档。`. Keep the current search, selected row, and quantity after success. Ensure `Hide()` deactivates both input fields before clearing the EventSystem selection.

- [ ] **Step 3: Write the target compatibility checker**

Create `tests/check-item-spawner-targets.ps1` with this complete implementation:

```powershell
param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\WulinSH',
    [string]$PluginPath = ''
)

$ErrorActionPreference = 'Stop'
$bepInEx = Join-Path $GameRoot 'BepInEx'
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $GameRoot 'BepInEx\plugins\ItemSpawner\ItemSpawner.dll'
}

$assemblyRoots = @(
    (Join-Path $bepInEx 'core'),
    (Join-Path $bepInEx 'interop'),
    (Split-Path -Parent $PluginPath)
)

[AppDomain]::CurrentDomain.add_AssemblyResolve({
    param($sender, $eventArgs)
    $assemblyName = [System.Reflection.AssemblyName]::new($eventArgs.Name).Name + '.dll'
    foreach ($root in $assemblyRoots) {
        $candidate = Join-Path $root $assemblyName
        if (Test-Path -LiteralPath $candidate) {
            return [System.Reflection.Assembly]::LoadFrom($candidate)
        }
    }
    return $null
})

function Load-Assembly([string]$relativePath) {
    [System.Reflection.Assembly]::LoadFrom((Join-Path $GameRoot $relativePath))
}

function Resolve-Type([System.Reflection.Assembly]$assembly, [string]$name) {
    $type = $assembly.GetType($name, $false)
    if ($null -eq $type) { throw "Missing type: $name" }
    return $type
}

function Assert-Member($check) {
    $type = Resolve-Type $check.Assembly $check.Type
    $flags = [System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
    if ($check.ContainsKey('Method')) {
        $members = @($type.GetMethods($flags) | Where-Object { $_.Name -eq $check.Method })
        $target = "$($check.Type).$($check.Method)"
    } else {
        $members = @($type.GetProperties($flags) | Where-Object { $_.Name -eq $check.Property })
        $target = "$($check.Type).$($check.Property)"
    }
    if ($members.Count -eq 0) { throw "Missing member: $target" }
    [PSCustomObject]@{ Target = $target; Count = $members.Count }
}

$gameAssembly = Load-Assembly 'BepInEx\interop\Assembly-CSharp.dll'
$modShareAssembly = Load-Assembly 'BepInEx\interop\ModShare.Runtime.dll'
$tmpAssembly = Load-Assembly 'BepInEx\interop\Unity.TextMeshPro.dll'
$unityUiAssembly = Load-Assembly 'BepInEx\interop\UnityEngine.UI.dll'
$bepInExCoreAssembly = Load-Assembly 'BepInEx\core\BepInEx.Core.dll'
$pluginAssembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $PluginPath))

$checks = @(
    @{ Assembly = $modShareAssembly; Type = 'BaseDataClass'; Method = 'GetGameData' },
    @{ Assembly = $modShareAssembly; Type = 'GameData.ItemDataScriptObject'; Property = 'ItemData' },
    @{ Assembly = $modShareAssembly; Type = 'GameData.ItemData'; Property = 'Uid' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.GameUtil'; Method = 'GetName' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.PlayerTeamManager'; Property = 'TeamInventory' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.GameItemPack'; Method = 'AddItem' },
    @{ Assembly = $tmpAssembly; Type = 'TMPro.TMP_InputField'; Property = 'text' },
    @{ Assembly = $unityUiAssembly; Type = 'UnityEngine.UI.ScrollRect'; Property = 'content' }
)

$checks | ForEach-Object { Assert-Member $_ } | Format-Table -AutoSize

$bepInPluginType = Resolve-Type $bepInExCoreAssembly 'BepInEx.BepInPlugin'
$pluginAttributes = @(
    foreach ($type in $pluginAssembly.GetTypes()) {
        $type.GetCustomAttributes($bepInPluginType, $false)
    }
)
if ($pluginAttributes.Count -ne 1) {
    throw "Expected one BepInPlugin attribute, found $($pluginAttributes.Count)."
}
if ($pluginAttributes[0].GUID -ne 'com.haxx.ItemSpawner') {
    throw "Unexpected plugin GUID: $($pluginAttributes[0].GUID)"
}

$flags = [System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
foreach ($type in $pluginAssembly.GetTypes()) {
    $null = $type.GetCustomAttributes($false)
    foreach ($method in $type.GetMethods($flags)) {
        $null = $method.GetCustomAttributes($false)
    }
}

Write-Host 'All ItemSpawner game API targets and plugin metadata resolved.'
```

- [ ] **Step 4: Update README with exact standalone instructions**

Add an `ItemSpawner` section covering:

- `F8` opens/closes the window and the key is configurable in `BepInEx/config/com.haxx.ItemSpawner.cfg`.
- Search accepts current-language names and IDs.
- Quantity range is `1–999`, default `1`.
- Generated items go to the team inventory with native feedback.
- Quest and hidden items can affect progression; back up saves first.
- Build command: `dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release`.
- Install path: `BepInEx\plugins\ItemSpawner\ItemSpawner.dll`.
- All four ItemSpawner test commands and the existing EnhanceGameplay checks.

- [ ] **Step 5: Run the complete source and compatibility suite**

```powershell
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
.\tests\check-item-spawner-catalog.ps1
.\tests\check-item-spawner-grant.ps1
.\tests\check-item-spawner-ui.ps1
.\tests\check-item-spawner-targets.ps1 -PluginPath .\src\ItemSpawner\bin\Release\net6.0\ItemSpawner.dll
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
.\tests\check-all-internal-kungfu-effects.ps1
git diff --check
```

Expected: both Release builds and every script succeed; `git diff --check` prints nothing.

- [ ] **Step 6: Commit complete interaction and documentation**

```powershell
git add -- README.md tests/check-item-spawner-ui.ps1 tests/check-item-spawner-targets.ps1 src/ItemSpawner/ItemSpawner/ItemSpawnerWindow.cs
git commit -m "Complete item spawner interaction"
```

---

### Task 6: Fresh Verification, Local Deployment, and Release Package

**Files:**
- Verify: `src/ItemSpawner/bin/Release/net6.0/ItemSpawner.dll`
- Deploy: `E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\plugins\ItemSpawner\ItemSpawner.dll`
- Create: `release-artifacts/ItemSpawner-v1.0.0.zip`

**Interfaces:**
- Consumes: the committed source tree, complete automated suite, and Release DLL.
- Produces: a hash-verified local installation and a directly extractable ZIP package.

- [ ] **Step 1: Run fresh Release verification**

```powershell
dotnet clean .\src\ItemSpawner\ItemSpawner.csproj -c Release
dotnet build .\src\ItemSpawner\ItemSpawner.csproj -c Release
dotnet build .\src\EnhanceGameplay\EnhanceGameplay.csproj -c Release
.\tests\check-item-spawner-catalog.ps1
.\tests\check-item-spawner-grant.ps1
.\tests\check-item-spawner-ui.ps1
.\tests\check-item-spawner-targets.ps1 -PluginPath .\src\ItemSpawner\bin\Release\net6.0\ItemSpawner.dll
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
.\tests\check-all-internal-kungfu-effects.ps1
.\tests\check-hook-targets.ps1 -PluginPath .\src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll
git diff --check
```

Expected: all builds/checks pass, no warnings/errors, and no whitespace errors.

- [ ] **Step 2: Back up and deploy the verified DLL**

Run this PowerShell block from the repository root:

```powershell
$runningGame = Get-Process -Name 'Wulin' -ErrorAction SilentlyContinue
if ($runningGame) { throw 'Close Wulin before deploying ItemSpawner.dll.' }
$source = Resolve-Path '.\src\ItemSpawner\bin\Release\net6.0\ItemSpawner.dll'
$pluginDir = 'E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\plugins\ItemSpawner'
$target = Join-Path $pluginDir 'ItemSpawner.dll'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
if (Test-Path -LiteralPath $target) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    Copy-Item -LiteralPath $target -Destination "$target.bak-$stamp"
}
Copy-Item -LiteralPath $source -Destination $target -Force
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($sourceHash -ne $targetHash) { throw 'Deployed ItemSpawner hash mismatch.' }
[PSCustomObject]@{ Deployed = $target; SHA256 = $targetHash }
```

Expected: target exists and SHA-256 equals the verified build.

- [ ] **Step 3: Build the directly extractable ZIP**

```powershell
$artifactRoot = Resolve-Path '.\release-artifacts'
$stage = Join-Path $artifactRoot 'ItemSpawner-v1.0.0'
$zip = Join-Path $artifactRoot 'ItemSpawner-v1.0.0.zip'
if (Test-Path -LiteralPath $stage) { throw "Staging path already exists: $stage" }
if (Test-Path -LiteralPath $zip) { throw "Release ZIP already exists: $zip" }
$pluginStage = Join-Path $stage 'BepInEx\plugins\ItemSpawner'
New-Item -ItemType Directory -Path $pluginStage -Force | Out-Null
Copy-Item -LiteralPath '.\src\ItemSpawner\bin\Release\net6.0\ItemSpawner.dll' -Destination $pluginStage
Copy-Item -LiteralPath '.\README.md' -Destination (Join-Path $stage 'README.md')
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
Get-Item -LiteralPath $zip | Select-Object FullName, Length, LastWriteTime
```

Expected: `release-artifacts/ItemSpawner-v1.0.0.zip` contains `BepInEx/plugins/ItemSpawner/ItemSpawner.dll` and `README.md`.

- [ ] **Step 4: Verify package contents and hashes**

```powershell
$zip = Resolve-Path '.\release-artifacts\ItemSpawner-v1.0.0.zip'
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $archive.Entries | Select-Object FullName, Length
    if (-not ($archive.Entries.FullName -contains 'BepInEx/plugins/ItemSpawner/ItemSpawner.dll')) {
        throw 'Package is missing ItemSpawner.dll.'
    }
} finally {
    $archive.Dispose()
}
Get-FileHash -LiteralPath $zip -Algorithm SHA256
git status --short --branch
```

Expected: required DLL entry is present; ZIP hash is reported; only the user's pre-existing untracked archive/screenshots remain because `release-artifacts/` is ignored.

- [ ] **Step 5: Report the manual game acceptance boundary**

Ask the user to restart the game, enter a save, press `F8`, search by Chinese name and ID, generate stackable and non-stackable items with boundary quantities, close the window, and inspect `BepInEx/LogOutput.log`. Do not claim in-game acceptance is complete until that check is performed.
