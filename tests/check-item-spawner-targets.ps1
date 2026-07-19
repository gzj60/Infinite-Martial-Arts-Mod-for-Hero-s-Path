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
    if ($null -eq $type) {
        throw "Missing type: $name"
    }
    return $type
}

function Assert-Member($check) {
    $type = Resolve-Type $check.Assembly $check.Type
    $flags = [System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
    if ($check.ContainsKey('Method')) {
        $members = @($type.GetMethods($flags) | Where-Object { $_.Name -eq $check.Method })
        $target = "$($check.Type).$($check.Method)"
    }
    else {
        $members = @($type.GetProperties($flags) | Where-Object { $_.Name -eq $check.Property })
        $target = "$($check.Type).$($check.Property)"
    }
    if ($members.Count -eq 0) {
        throw "Missing member: $target"
    }
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
