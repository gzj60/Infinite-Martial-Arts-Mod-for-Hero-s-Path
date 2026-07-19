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

Require-Pattern $quantity 'int\.TryParse' 'Quantity must be parsed as an integer.'
Require-Pattern $quantity 'quantity\s*<\s*1\s*\|\|\s*quantity\s*>\s*999' 'Quantity must be limited to 1 through 999.'
Require-Pattern $grant 'PlayerTeamManager\.Instance' 'Grant service must obtain the player team manager.'
Require-Pattern $grant '\.TeamInventory' 'Grant service must target the team inventory.'
Require-Pattern $grant 'AddItem\(entry\.Id,\s*quantity,\s*true\)' 'Grant service must request native item-get feedback.'
Require-Pattern $grant 'catch\s*\(Exception\s+ex\)' 'Grant failures must be converted into a result.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) item grant check(s) failed."
}

Write-Host 'Item spawner grant checks passed.'
