param(
    [string]$GameRoot = 'E:\SteamLibrary\steamapps\common\WulinSH',
    [string]$PluginPath = ''
)

$ErrorActionPreference = 'Stop'

$bepInEx = Join-Path $GameRoot 'BepInEx'
if ([string]::IsNullOrWhiteSpace($PluginPath)) {
    $PluginPath = Join-Path $GameRoot 'BepInEx\plugins\EnhanceGameplay\EnhanceGameplay.dll'
}

$assemblyRoots = @(
    (Join-Path $bepInEx 'core'),
    (Join-Path $bepInEx 'interop'),
    (Join-Path $bepInEx 'plugins\EnhanceGameplay'),
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

function Assert-Method([System.Reflection.Assembly]$assembly, [string]$typeName, [string]$methodName) {
    $type = Resolve-Type $assembly $typeName
    $flags = [System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
    $methods = @($type.GetMethods($flags) | Where-Object { $_.Name -eq $methodName })
    if ($methods.Count -eq 0) {
        throw "Missing method: $typeName.$methodName"
    }

    [PSCustomObject]@{
        Target = "$typeName.$methodName"
        Count = $methods.Count
    }
}

$gameAssembly = Load-Assembly 'BepInEx\interop\Assembly-CSharp.dll'
$unityUiAssembly = Load-Assembly 'BepInEx\interop\UnityEngine.UI.dll'
$unityCoreAssembly = Load-Assembly 'BepInEx\interop\UnityEngine.CoreModule.dll'
$pluginAssembly = [System.Reflection.Assembly]::LoadFrom($PluginPath)

$checks = @(
    @{ Assembly = $unityUiAssembly; Type = 'UnityEngine.UI.CanvasScaler'; Method = 'Handle' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.BattleUI'; Method = 'OnSpeedButtonClickHandler' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.BattleUI'; Method = 'SwitchSpeed' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.Role'; Method = 'UpdateSpeed' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.MiningBatchUI'; Method = 'OnPlayEnd' },
    @{ Assembly = $unityCoreAssembly; Type = 'UnityEngine.Random'; Method = 'Range' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.StateMachine.StartSpInteractiveActionNode'; Method = 'InvokeAction' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.GameCharacterInstance'; Method = 'CouldLearnKungfu' },
    @{ Assembly = $gameAssembly; Type = 'UIKongfuPanel'; Method = 'InitLeftPanel' },
    @{ Assembly = $gameAssembly; Type = 'WuLin.UISlider'; Method = 'InitPanel' }
)

$resolved = foreach ($check in $checks) {
    Assert-Method $check.Assembly $check.Type $check.Method
}

$resolved | Format-Table -AutoSize

$flags = [System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
foreach ($type in $pluginAssembly.GetTypes()) {
    foreach ($method in $type.GetMethods($flags)) {
        try {
            $null = $method.GetCustomAttributes($false)
        }
        catch {
            throw "Broken custom attributes on $($type.FullName).$($method.Name): $($_.Exception.Message)"
        }
    }
}

Write-Host "All hook targets and Harmony metadata resolved."
