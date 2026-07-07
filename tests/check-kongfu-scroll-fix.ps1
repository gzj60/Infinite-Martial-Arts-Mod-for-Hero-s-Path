param(
    [string]$SourceRoot = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $SourceRoot = Join-Path $repoRoot 'src\EnhanceGameplay\EnhanceGameplay'
}

$modComponentPath = Join-Path $SourceRoot 'ModComponent.cs'
$martialPatchPath = Join-Path $SourceRoot 'MartialNumPatch.cs'

$modComponent = Get-Content -Raw -LiteralPath $modComponentPath
$martialPatch = Get-Content -Raw -LiteralPath $martialPatchPath
$failures = @()

function Require-Pattern {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -notmatch $Pattern) {
        $script:failures += $Message
    }
}

Require-Pattern $modComponent 'SetupKungfuScrollRect' 'ModComponent should isolate kungfu ScrollRect setup in a helper.'
Require-Pattern $modComponent 'TryInitializeMartialPanel' 'Martial panel setup should run independently from battle speed-button setup.'
Require-Pattern $modComponent 'martialPanelInitialized' 'Martial panel setup should have its own initialization guard.'
if ($modComponent -match 'TryInitializeBattleSpeedButton|battleSpeedInitialized|BattleUI|playerSpeedUp') {
    $failures += 'Kungfu scroll setup should no longer depend on battle speed-button code.'
}
Require-Pattern $modComponent '\.vertical\s*=\s*true' 'Kungfu ScrollRect should enable vertical scrolling.'
Require-Pattern $modComponent '\.horizontal\s*=\s*false' 'Kungfu ScrollRect should disable horizontal scrolling.'
Require-Pattern $modComponent 'MovementType\.Clamped' 'Kungfu ScrollRect should use clamped movement.'
Require-Pattern $modComponent 'RefreshLearnedSkillScrollArea' 'Kungfu content height should be refreshed after replacing ScrollRect content.'
Require-Pattern $modComponent 'Canvas\.ForceUpdateCanvases' 'Kungfu layout changes should force a canvas/layout refresh.'

Require-Pattern $martialPatch 'RefreshLearnedSkillScrollArea' 'InitLeftPanel patch should refresh scroll content after game UI rebuilds the list.'
Require-Pattern $martialPatch 'TryBindMoveForwardButton' 'MoveForward rebinding should be guarded in a helper.'
Require-Pattern $martialPatch 'GetComponent<Button>\(\)' 'MoveForward rebinding should verify the Button component.'
Require-Pattern $martialPatch 'GetComponent<UILearnedSkillPanel>\(\)' 'MoveForward rebinding should verify UILearnedSkillPanel before reading data.'
Require-Pattern $martialPatch '==\s*\(Object\)null' 'MoveForward rebinding should skip missing Unity objects.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) kungfu scroll regression check(s) failed."
}

Write-Host 'Kungfu scroll regression checks passed.'
