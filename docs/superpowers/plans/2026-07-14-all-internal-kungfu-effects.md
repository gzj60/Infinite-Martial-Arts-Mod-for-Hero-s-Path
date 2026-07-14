# All Internal Kungfu Effects Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every learned internal kungfu's main-effect battle events apply to the player, companions, and player-side allies without changing enemies, save data, or out-of-battle permanent stats.

**Architecture:** Add one focused Harmony postfix around `BattleActor.CreateInternalKungfuEffectEvents`. Keep the original active-internal event first, then temporarily expose each other learned internal to the original game method and append its event chain, with a thread-local recursion guard and guaranteed state restoration.

**Tech Stack:** C# 10 / .NET 6, BepInEx 6 IL2CPP, Harmony, Il2CppInterop, PowerShell regression checks.

## Global Constraints

- Apply only when `BattleActor.ServeBattleTeam` is `BattleTeamEnum.Player` or `BattleTeamEnum.Allie`.
- Never apply the expansion to enemies or actors currently serving the enemy side.
- Limit expansion to the seven `BattleInternalKungfu*` stages numbered 230 through 236.
- Do not call `GameCharacterInstance.SetActiveInternalKungfu` or persist a different active internal kungfu.
- Restore `activedInternalKungfu` and `m_activedInternalKunfuId` in `finally` after every temporary switch.
- Preserve the game's existing duplicate, global-unique, and single-trigger effect restrictions.
- Keep the existing unlimited-kungfu, scrolling, and MoveForward behavior unchanged.
- Do not create or overwrite a GitHub Release in this implementation.

---

### Task 1: Friendly All-Internal Battle Event Expansion

**Files:**
- Create: `tests/check-all-internal-kungfu-effects.ps1`
- Create: `src/EnhanceGameplay/EnhanceGameplay/InternalKungfuPatch.cs`
- Modify: `src/EnhanceGameplay/EnhanceGameplay/BepInExLoader.cs`

**Interfaces:**
- Consumes: `BattleActor.CreateInternalKungfuEffectEvents(DynamicModifier.DynamicModifierActiveStage)` and `BattleActor.info.characterInstance.GetInternalKungku()` from the game interop assembly.
- Produces: `InternalKungfuPatch.CreateInternalKungfuEffectEvents_Postfix(BattleActor, DynamicModifier.DynamicModifierActiveStage, ref InternalKungfuEffectEvent)` registered by `BepInExLoader`.

- [ ] **Step 1: Write the failing source regression check**

Create `tests/check-all-internal-kungfu-effects.ps1`:

```powershell
param(
    [string]$SourceRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $SourceRoot = Join-Path $repoRoot 'src\EnhanceGameplay\EnhanceGameplay'
}

$patchPath = Join-Path $SourceRoot 'InternalKungfuPatch.cs'
$loaderPath = Join-Path $SourceRoot 'BepInExLoader.cs'
$patchSource = if (Test-Path -LiteralPath $patchPath) {
    Get-Content -Raw -LiteralPath $patchPath
} else {
    ''
}
$loaderSource = Get-Content -Raw -LiteralPath $loaderPath
$failures = @()

function Require-Pattern {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ($Text -notmatch $Pattern) {
        $script:failures += $Message
    }
}

function Forbid-Pattern {
    param([string]$Text, [string]$Pattern, [string]$Message)
    if ($Text -match $Pattern) {
        $script:failures += $Message
    }
}

Require-Pattern $patchSource 'HarmonyPatch\(typeof\(BattleActor\),\s*"CreateInternalKungfuEffectEvents"\)' 'Patch should target BattleActor.CreateInternalKungfuEffectEvents.'
Require-Pattern $patchSource 'ServeBattleTeam' 'Patch should filter by the actor current serving team.'
Require-Pattern $patchSource 'BattleTeamEnum\.Player' 'Patch should include player-side actors.'
Require-Pattern $patchSource 'BattleTeamEnum\.Allie' 'Patch should include player-side allies.'
Forbid-Pattern $patchSource 'BattleTeamEnum\.Enemy' 'Patch should not opt enemy actors into all-internal effects.'
Require-Pattern $patchSource '\[ThreadStatic\]' 'Patch should use a thread-local recursion guard.'
Require-Pattern $patchSource 'GetInternalKungku\(\)' 'Patch should enumerate all learned internal kungfu.'
Require-Pattern $patchSource 'CreateInternalKungfuEffectEvents\(dynamicModifierActiveStage\)' 'Patch should reuse the original game event builder.'
Require-Pattern $patchSource 'FindLast\(\).*LinkWith|LinkWith\(' 'Patch should append additional event chains.'
Require-Pattern $patchSource 'finally' 'Temporary active-internal state should be restored in finally.'
Require-Pattern $patchSource 'activedInternalKungfu\s*=\s*originalActive' 'Patch should restore the active internal object.'
Require-Pattern $patchSource 'm_activedInternalKunfuId\s*=\s*originalActiveId' 'Patch should restore the active internal ID.'
Forbid-Pattern $patchSource 'SetActiveInternalKungfu\s*\(' 'Patch should not invoke persistent active-internal switching.'
Require-Pattern $loaderSource 'PatchAll\(typeof\(InternalKungfuPatch\)\)' 'Loader should register InternalKungfuPatch.'

$stages = @(
    'BattleInternalKungfuEnterBattle',
    'BattleInternalKungfuBeforeAttack',
    'BattleInternalKungfuAfterAttack',
    'BattleInternalKungfuBeforeHit',
    'BattleInternalKungfuAfterHit',
    'BattleInternalKungfuAfterAction',
    'BattleInternalKungfuSwitch'
)
foreach ($stage in $stages) {
    Require-Pattern $patchSource ([regex]::Escape($stage)) "Patch should allow stage $stage."
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) all-internal kungfu effect check(s) failed."
}

Write-Host 'All-internal kungfu effect checks passed.'
```

- [ ] **Step 2: Run the regression check and verify RED**

Run:

```powershell
.\tests\check-all-internal-kungfu-effects.ps1
```

Expected: FAIL with missing target, team filter, stages, recursion guard, restoration, event linking, and loader registration because `InternalKungfuPatch.cs` does not exist yet.

- [ ] **Step 3: Add the minimal Harmony implementation**

Create `src/EnhanceGameplay/EnhanceGameplay/InternalKungfuPatch.cs`:

```csharp
using System;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using WuLin;

namespace EnhanceGameplay;

public static class InternalKungfuPatch
{
	[ThreadStatic]
	private static bool creatingAdditionalInternalEvents;

	[HarmonyPatch(typeof(BattleActor), "CreateInternalKungfuEffectEvents")]
	[HarmonyPostfix]
	public static void CreateInternalKungfuEffectEvents_Postfix(
		BattleActor __instance,
		DynamicModifier.DynamicModifierActiveStage dynamicModifierActiveStage,
		ref InternalKungfuEffectEvent __result)
	{
		if (creatingAdditionalInternalEvents || __instance == null ||
			!IsFriendlyActor(__instance) || !IsInternalKungfuBattleStage(dynamicModifierActiveStage))
		{
			return;
		}

		BattleActorCreateInfo info = __instance.info;
		GameCharacterInstance character = info == null ? null : info.characterInstance;
		if (character == null)
		{
			return;
		}

		List<KungfuInstance> internalKungfu = character.GetInternalKungku();
		if (internalKungfu == null || internalKungfu.Count == 0)
		{
			return;
		}

		BuildAdditionalEvents(__instance, character, internalKungfu, dynamicModifierActiveStage, ref __result);
	}

	private static bool IsFriendlyActor(BattleActor actor)
	{
		BattleTeamEnum team = actor.ServeBattleTeam;
		return team == BattleTeamEnum.Player || team == BattleTeamEnum.Allie;
	}

	private static bool IsInternalKungfuBattleStage(DynamicModifier.DynamicModifierActiveStage stage)
	{
		return stage switch
		{
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuEnterBattle => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuBeforeAttack => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuAfterAttack => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuBeforeHit => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuAfterHit => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuAfterAction => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuSwitch => true,
			_ => false
		};
	}

	private static void BuildAdditionalEvents(
		BattleActor actor,
		GameCharacterInstance character,
		List<KungfuInstance> internalKungfu,
		DynamicModifier.DynamicModifierActiveStage dynamicModifierActiveStage,
		ref InternalKungfuEffectEvent result)
	{
		KungfuInstance originalActive = character.activedInternalKungfu;
		for (int i = 0; i < internalKungfu.Count; i++)
		{
			KungfuInstance kungfu = internalKungfu[i];
			if (kungfu == null || kungfu == originalActive)
			{
				continue;
			}

			try
			{
				InternalKungfuEffectEvent additional = BuildEventForKungfu(
					actor,
					character,
					kungfu,
					dynamicModifierActiveStage);
				result = AppendEventChain(result, additional);
			}
			catch (Exception ex)
			{
				BepInExLoader.log?.LogError(
					$"Failed to build internal kungfu effect for {kungfu.TempleteUid}: {ex}");
			}
		}
	}

	private static InternalKungfuEffectEvent BuildEventForKungfu(
		BattleActor actor,
		GameCharacterInstance character,
		KungfuInstance kungfu,
		DynamicModifier.DynamicModifierActiveStage dynamicModifierActiveStage)
	{
		KungfuInstance originalActive = character.activedInternalKungfu;
		int originalActiveId = character.m_activedInternalKunfuId;
		try
		{
			character.activedInternalKungfu = kungfu;
			character.m_activedInternalKunfuId = kungfu.TempleteUid;
			creatingAdditionalInternalEvents = true;
			return actor.CreateInternalKungfuEffectEvents(dynamicModifierActiveStage);
		}
		finally
		{
			creatingAdditionalInternalEvents = false;
			character.activedInternalKungfu = originalActive;
			character.m_activedInternalKunfuId = originalActiveId;
		}
	}

	private static InternalKungfuEffectEvent AppendEventChain(
		InternalKungfuEffectEvent result,
		InternalKungfuEffectEvent additional)
	{
		if (additional == null)
		{
			return result;
		}
		if (result == null)
		{
			return additional;
		}

		BattleFieldEvent tail = result.FindLast();
		BattleFieldEvent head = additional.FindFirst();
		if (tail != null && head != null)
		{
			tail.LinkWith(head);
		}
		return result;
	}
}
```

In `src/EnhanceGameplay/EnhanceGameplay/BepInExLoader.cs`, register the new patch immediately after the existing martial patch:

```csharp
harmony.PatchAll(typeof(MartialNumPatch));
harmony.PatchAll(typeof(InternalKungfuPatch));
```

- [ ] **Step 4: Run the focused check and verify GREEN**

Run:

```powershell
.\tests\check-all-internal-kungfu-effects.ps1
```

Expected: `All-internal kungfu effect checks passed.`

- [ ] **Step 5: Build and run the existing regression checks**

Run:

```powershell
dotnet build .\src\EnhanceGameplay\EnhanceGameplay.csproj -c Release
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
```

Expected: build succeeds with 0 warnings and 0 errors; both existing checks pass.

- [ ] **Step 6: Commit the tested behavior**

```powershell
git add -- tests/check-all-internal-kungfu-effects.ps1 src/EnhanceGameplay/EnhanceGameplay/InternalKungfuPatch.cs src/EnhanceGameplay/EnhanceGameplay/BepInExLoader.cs
git commit -m "Add friendly all-internal kungfu effects"
```

### Task 2: Hook Compatibility, Version, and Documentation

**Files:**
- Modify: `tests/check-hook-targets.ps1`
- Modify: `src/EnhanceGameplay/EnhanceGameplay/BepInExLoader.cs`
- Modify: `src/EnhanceGameplay/EnhanceGameplay/MyPluginInfo.cs`
- Modify: `src/EnhanceGameplay/Properties/AssemblyInfo.cs`
- Modify: `README.md`

**Interfaces:**
- Consumes: built `EnhanceGameplay.dll` and the current game's `Assembly-CSharp.dll`.
- Produces: plugin metadata version `1.1.0` and a hook resolver entry for `WuLin.BattleActor.CreateInternalKungfuEffectEvents`.

- [ ] **Step 1: Extend the hook target compatibility check**

Add the new game method to `$checks` in `tests/check-hook-targets.ps1`:

```powershell
$checks = @(
    @{ Assembly = $unityUiAssembly; Type = 'UnityEngine.UI.CanvasScaler'; Method = 'Handle' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.GameCharacterInstance'; Method = 'CouldLearnKungfu' },
    @{ Assembly = $gameAssembly; Type = 'UIKongfuPanel'; Method = 'InitLeftPanel' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.BattleActor'; Method = 'CreateInternalKungfuEffectEvents' }
)
```

- [ ] **Step 2: Bump all plugin metadata to 1.1.0**

In `src/EnhanceGameplay/EnhanceGameplay/BepInExLoader.cs`:

```csharp
[BepInPlugin("com.haxx.EnhanceGameplay", "InfiniteMartialArts", "1.1.0")]
```

```csharp
public const string VERSION = "1.1.0";
```

In `src/EnhanceGameplay/EnhanceGameplay/MyPluginInfo.cs`:

```csharp
public const string PLUGIN_VERSION = "1.1.0";
```

In `src/EnhanceGameplay/Properties/AssemblyInfo.cs`:

```csharp
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0")]
[assembly: AssemblyVersion("1.1.0.0")]
```

- [ ] **Step 3: Update README behavior and verification instructions**

Add these bullets under `功能简介` in `README.md`:

```markdown
- 主角、队友和我方友军已学习的全部内功，其主内功战斗效果无需设置即可同时生效。
- 敌人仍按原版规则只触发当前主内功；战斗外常驻属性不会重复结算。
```

Add the focused check to `可选检查`:

```powershell
.\tests\check-all-internal-kungfu-effects.ps1
```

Change the loading-log example to:

```text
Loading [InfiniteMartialArts 1.1.0]
```

- [ ] **Step 4: Rebuild and verify the new hook metadata**

Run:

```powershell
dotnet build .\src\EnhanceGameplay\EnhanceGameplay.csproj -c Release
.\tests\check-hook-targets.ps1 -PluginPath .\src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll
```

Expected: build succeeds with 0 warnings and 0 errors; the hook table includes `WuLin.BattleActor.CreateInternalKungfuEffectEvents`; Harmony metadata resolves without exceptions.

- [ ] **Step 5: Commit compatibility and documentation changes**

```powershell
git add -- tests/check-hook-targets.ps1 src/EnhanceGameplay/EnhanceGameplay/BepInExLoader.cs src/EnhanceGameplay/EnhanceGameplay/MyPluginInfo.cs src/EnhanceGameplay/Properties/AssemblyInfo.cs README.md
git commit -m "Document and verify all-internal effects"
```

### Task 3: Full Verification and Local Game Deployment

**Files:**
- Verify: `src/EnhanceGameplay/bin/Release/net6.0/EnhanceGameplay.dll`
- Deploy: `E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\plugins\EnhanceGameplay\EnhanceGameplay.dll`

**Interfaces:**
- Consumes: the Release build and all PowerShell regression scripts.
- Produces: a timestamped backup of the installed DLL and the verified 1.1.0 DLL in the game plugin directory.

- [ ] **Step 1: Run the complete verification suite from a fresh Release build**

Run:

```powershell
dotnet build .\src\EnhanceGameplay\EnhanceGameplay.csproj -c Release
.\tests\check-all-internal-kungfu-effects.ps1
.\tests\check-infinite-martial-only.ps1
.\tests\check-kongfu-scroll-fix.ps1
.\tests\check-hook-targets.ps1 -PluginPath .\src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll
git diff --check
```

Expected: Release build has 0 warnings and 0 errors; all four checks pass; `git diff --check` has no output.

- [ ] **Step 2: Back up and deploy the verified DLL**

Run:

```powershell
$source = Resolve-Path '.\src\EnhanceGameplay\bin\Release\net6.0\EnhanceGameplay.dll'
$target = 'E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\plugins\EnhanceGameplay\EnhanceGameplay.dll'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "$target.bak-$stamp"
Copy-Item -LiteralPath $target -Destination $backup
Copy-Item -LiteralPath $source -Destination $target -Force
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($sourceHash -ne $targetHash) {
    throw 'Deployed DLL hash does not match the verified build.'
}
[PSCustomObject]@{
    Backup = $backup
    Deployed = $target
    SHA256 = $targetHash
}
```

Expected: a timestamped `.bak-*` file is created and source/deployed SHA256 values match.

- [ ] **Step 3: Verify repository state and report the manual game check**

Run:

```powershell
git status --short --branch
git log -3 --oneline --decorate
```

Expected: only the user's pre-existing untracked screenshots/archive remain; the two implementation commits are at `HEAD`.

Manual game verification after restarting the game:

1. Enter battle with the main character and at least one companion who knows two internal kungfu skills.
2. Keep only one internal kungfu marked as the main internal.
3. Confirm a distinctive effect from the non-main internal triggers at its documented battle stage.
4. Confirm the same behavior on the companion.
5. Confirm an enemy with multiple internals still only triggers its selected main internal.
6. Inspect `E:\SteamLibrary\steamapps\common\WulinSH\BepInEx\LogOutput.log` for errors mentioning `InternalKungfuPatch`.

Do not claim the in-game behavior is confirmed until the user has restarted the game and performed this check.
