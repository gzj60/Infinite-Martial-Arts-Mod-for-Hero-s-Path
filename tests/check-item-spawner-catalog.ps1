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
