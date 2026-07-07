using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.UI;

namespace EnhanceGameplay;

[BepInPlugin("com.haxx.EnhanceGameplay", "InfiniteMartialArts", "1.0.0")]
public class BepInExLoader : BasePlugin
{
	public const string GUID = "com.haxx.EnhanceGameplay";

	public const string NAME = "InfiniteMartialArts";

	public const string AUTHOR = "Haxx";

	public const string VERSION = "1.0.0";

	public static ManualLogSource log;

	public BepInExLoader()
	{
		AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
		log = ((BasePlugin)this).Log;
	}

	private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
	{
		log.LogError((object)("\r\n\r\nUnhandled Exception:" + (e.ExceptionObject as Exception)));
	}

	public override void Load()
	{
		log.LogMessage((object)"Registering infinite martial arts Il2Cpp types.");
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<Bootstrapper>();
			ClassInjector.RegisterTypeInIl2Cpp<ModComponent>();
		}
		catch (Exception ex)
		{
			log.LogError((object)("FAILED to register Il2Cpp types: " + ex));
		}

		Bootstrapper.Create("InfiniteMartialArtsBootstrapper");

		try
		{
			Harmony harmony = new Harmony("com.haxx.EnhanceGameplay");
			MethodInfo canvasScalerHandle = AccessTools.Method(typeof(CanvasScaler), "Handle", (Type[])null, (Type[])null);
			MethodInfo bootstrapperUpdate = AccessTools.Method(typeof(Bootstrapper), "Update", (Type[])null, (Type[])null);
			harmony.Patch((MethodBase)canvasScalerHandle, (HarmonyMethod)null, new HarmonyMethod(bootstrapperUpdate), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.PatchAll(typeof(MartialNumPatch));
		}
		catch (Exception ex)
		{
			log.LogError((object)("FAILED to apply infinite martial arts hooks: " + ex));
		}
	}
}
