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
