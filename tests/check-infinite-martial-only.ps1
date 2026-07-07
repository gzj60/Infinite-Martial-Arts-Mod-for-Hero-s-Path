param(
    [string]$SourceRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $SourceRoot = Join-Path $repoRoot 'src\EnhanceGameplay'
}

$files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Include '*.cs'
$source = ($files | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
$relativeFiles = $files | ForEach-Object {
    [System.IO.Path]::GetRelativePath($SourceRoot, $_.FullName).Replace('\', '/')
}
$failures = @()

function Forbid-Pattern {
    param(
        [string]$Pattern,
        [string]$Message
    )

    if ($source -match $Pattern) {
        $script:failures += $Message
    }
}

function Forbid-File {
    param(
        [string]$Path,
        [string]$Message
    )

    if ($relativeFiles -contains $Path) {
        $script:failures += $Message
    }
}

function Require-Pattern {
    param(
        [string]$Pattern,
        [string]$Message
    )

    if ($source -notmatch $Pattern) {
        $script:failures += $Message
    }
}

Forbid-File 'EnhanceGameplay/ToolPatch.cs' 'Tool durability/mining patch file should be removed.'
Forbid-File 'EnhanceGameplay/MiscPatch.cs' 'Battle and roaming speed patch file should be removed.'
Forbid-File 'EnhanceGameplay/InteractionPatch.cs' 'Interaction/gifting patch file should be removed.'
Forbid-File 'EnhanceGameplay/EasyQTEPatch.cs' 'Easy QTE patch file should be removed.'
Forbid-File 'EnhanceGameplay.UI/WindowDragHandler.cs' 'Unused UI helper should be removed.'
Forbid-File 'EnhanceGameplay/Il2CppTypeSupport.cs' 'Unused Il2Cpp reflection helper should be removed.'

Forbid-Pattern 'PatchAll\(typeof\((ToolPatch|MiscPatch|InteractionPatch|EasyQTEPatch)\)\)' 'Loader should not patch removed feature classes.'
Forbid-Pattern 'ConfigEntry<[^>]+>\s+(toolUnbreakable|tradeFreely|easyQTE|batchHotKey)' 'Loader should not expose removed feature config entries.'
Forbid-Pattern 'ToolUnbreakable|TradeFreely|EasyQTE|Batch' 'Removed feature config keys should not remain.'
Forbid-Pattern 'BattleUI|Role|MiningBatchUI|UnityEngine\.Random|UISlider|StartSpInteractiveActionNode' 'Removed feature target game types should not be referenced.'
Forbid-Pattern 'Time\.timeScale|playerSpeedUp|battleSpeed|isBatch|isMinning|TryInitializeBattleSpeedButton|battle speed' 'Removed runtime speed/mining state should not remain.'
Forbid-Pattern 'RegisterTypeInIl2Cpp<WindowDragHandler>|Il2CppTypeSupport\.Initialize' 'Removed helper types should not be initialized.'

Require-Pattern 'PatchAll\(typeof\(MartialNumPatch\)\)' 'Loader should still patch MartialNumPatch.'
Require-Pattern 'GameCharacterInstance.+CouldLearnKungfu|CouldLearnKungfu.+GameCharacterInstance' 'Infinite martial arts hook should remain.'
Require-Pattern 'UIKongfuPanel.+InitLeftPanel|InitLeftPanel.+UIKongfuPanel' 'Kungfu list UI hook should remain.'
Require-Pattern 'TryInitializeMartialPanel' 'Kungfu scroll UI initialization should remain.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) infinite martial only check(s) failed."
}

Write-Host 'Infinite martial arts only checks passed.'
