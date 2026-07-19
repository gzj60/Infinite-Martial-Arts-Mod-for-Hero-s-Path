param([string]$SourceRoot = '')

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\ItemSpawner\ItemSpawner'
}

$files = Get-ChildItem -LiteralPath $SourceRoot -File -Filter '*.cs' -ErrorAction SilentlyContinue
$source = ($files | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
$uiFactorySource = Get-Content -Raw -LiteralPath (Join-Path $SourceRoot 'UiFactory.cs')
$windowSource = Get-Content -Raw -LiteralPath (Join-Path $SourceRoot 'ItemSpawnerWindow.cs')
$failures = @()
function Require-Pattern([string]$Pattern, [string]$Message) {
    if ($source -notmatch $Pattern) { $script:failures += $Message }
}
function Require-FilePattern([string]$FileSource, [string]$Pattern, [string]$Message) {
    if ($FileSource -notmatch $Pattern) { $script:failures += $Message }
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
Require-FilePattern $uiFactorySource 'IsCjkCharacter' 'Game-font detection must explicitly identify CJK text.'
Require-FilePattern $uiFactorySource '\\u3400' 'Game-font detection must include the CJK Extension A range.'
Require-FilePattern $uiFactorySource '\\u9fff' 'Game-font detection must include the unified CJK range.'
Require-FilePattern $uiFactorySource 'AddComponent<RectMask2D>\(\)' 'Input viewport must clip long text.'
Require-Pattern 'class\s+VirtualizedItemList' 'UI must pool item rows.'
Require-Pattern 'const\s+int\s+PoolSize\s*=\s*16' 'UI must use a bounded 16-row pool.'
Require-Pattern 'Mathf\.FloorToInt\([^\)]*anchoredPosition\.y\s*/\s*RowHeight' 'Visible binding must follow scroll position.'
Require-Pattern 'SetItems\(List<ItemEntry>\s+items\)' 'Virtualized list must accept filtered results.'
Require-Pattern 'SearchInput\.onValueChanged' 'Search must refresh while typing.'
Require-Pattern 'catalog\.Search\(SearchInput\.text\)' 'Search must filter the loaded catalog.'
Require-Pattern 'QuantityInput\.text\s*=\s*"1"' 'Quantity must default to 1.'
Require-Pattern 'QuantityInput\.characterLimit\s*=\s*3' 'Quantity input must be limited to three characters.'
Require-Pattern 'TMP_InputField\.ContentType\.IntegerNumber' 'Quantity input must request integer input.'
Require-Pattern 'GenerateButton\.onClick' 'Generate button must have a click handler.'
Require-Pattern 'grantService\.Grant\(list\.Selected,\s*quantity\)' 'Click must grant the selected item and validated quantity.'
Require-Pattern '任务或隐藏物品可能影响存档' 'Window must show the confirmed save-risk warning.'
Require-FilePattern $windowSource 'nextStateRefresh' 'Generate readiness must be refreshed while the window remains open.'
Require-FilePattern $windowSource 'QuantityParser\.TryParse\(QuantityInput\.text,\s*out\s+_,\s*out\s+string\s+quantityError\)' 'Invalid quantity input must retain its validation message.'
Require-FilePattern $windowSource 'StatusText\.text\s*=\s*quantityError' 'Invalid quantity input must be shown while the button is disabled.'
Require-FilePattern $windowSource 'grantService\.IsReady\(out\s+string\s+readinessReason\)' 'Unavailable save state must provide a visible readiness reason.'
Require-FilePattern $windowSource 'StatusText\.text\s*=\s*readinessReason' 'Unavailable save state must be shown in the status bar.'
Require-FilePattern $windowSource 'float\s+maximumY\s*=\s*canvasHeight\s*\*\s*0\.5f\s*-\s*WindowHeight\s*\*\s*0\.5f' 'Top drag bound must keep the title bar on screen.'
Forbid-Pattern 'Time\.timeScale' 'Window must not pause or accelerate the game.'
Forbid-Pattern 'UniverseLib' 'Standalone plugin must not depend on UniverseLib.'

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) item spawner UI check(s) failed."
}
Write-Host 'Item spawner UI checks passed.'
