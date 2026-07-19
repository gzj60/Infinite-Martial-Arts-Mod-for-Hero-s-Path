using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace ItemSpawner;

[BepInPlugin("com.haxx.ItemSpawner", "ItemSpawner", "1.0.0")]
public sealed class ItemSpawnerPlugin : BasePlugin
{
    private ItemSpawnerRuntime runtime;

    internal static ConfigEntry<KeyCode> ToggleKey { get; private set; }
    internal static ManualLogSource Logger { get; private set; }

    public override void Load()
    {
        Logger = base.Log;
        System.AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        ToggleKey = Config.Bind("General", "ToggleKey", KeyCode.F8, "打开或关闭物品生成器窗口。");
        runtime = AddComponent<ItemSpawnerRuntime>();
        Logger.LogMessage("ItemSpawner 1.0.0 loaded. Press F8 to toggle the window.");
    }

    public override bool Unload()
    {
        System.AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        if (runtime != null)
        {
            UnityEngine.Object.Destroy(runtime);
            runtime = null;
        }
        return true;
    }

    private static void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs args)
    {
        Logger?.LogError($"Unhandled ItemSpawner exception: {args.ExceptionObject}");
    }
}
