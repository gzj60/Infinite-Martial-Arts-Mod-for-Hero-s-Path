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
