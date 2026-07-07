using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using EnhanceGameplay.UI;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.UI;

namespace EnhanceGameplay;

[BepInPlugin("com.haxx.EnhanceGameplay", "EnhanceGameplay", "1.0.0")]
public class BepInExLoader : BasePlugin
{
	public const string GUID = "com.haxx.EnhanceGameplay";

	public const string NAME = "EnhanceGameplay";

	public const string AUTHOR = "Haxx";

	public const string VERSION = "1.0.0";

	public static ManualLogSource log;

	public static ConfigEntry<KeyCode> batchHotKey;

	public static ConfigEntry<bool> toolUnbreakable;

	public static ConfigEntry<bool> tradeFreely;

	public static ConfigEntry<bool> easyQTE;

	public static ConfigEntry<bool> martialNum;

	public BepInExLoader()
	{
		AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
		Application.runInBackground = true;
		log = ((BasePlugin)this).Log;
	}

	private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
	{
		log.LogError((object)("\r\n\r\nUnhandled Exception:" + (e.ExceptionObject as Exception).ToString()));
	}

	public override void Load()
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Expected O, but got Unknown
		//IL_0072: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00a2: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		//IL_00d2: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected O, but got Unknown
		//IL_0102: Expected O, but got Unknown
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Expected O, but got Unknown
		log.LogMessage((object)"Registering C# Type's in Il2Cpp");
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<Bootstrapper>();
			ClassInjector.RegisterTypeInIl2Cpp<ModComponent>();
			ClassInjector.RegisterTypeInIl2Cpp<WindowDragHandler>();
		}
		catch
		{
			log.LogError((object)"FAILED to Register Il2Cpp Type!");
		}
		Bootstrapper.Create("BootStrapperGO");
		toolUnbreakable = ((BasePlugin)this).Config.Bind<bool>(new ConfigDefinition("Settings", "ToolUnbreakable"), true, new ConfigDescription("工具不损坏", (AcceptableValueBase)null, Array.Empty<object>()));
		tradeFreely = ((BasePlugin)this).Config.Bind<bool>(new ConfigDefinition("Settings", "TradeFreely"), true, new ConfigDescription("送礼无好感限制", (AcceptableValueBase)null, Array.Empty<object>()));
		easyQTE = ((BasePlugin)this).Config.Bind<bool>(new ConfigDefinition("Settings", "EasyQTE"), true, new ConfigDescription("简单的QTE（偷窃/下毒/刺杀", (AcceptableValueBase)null, Array.Empty<object>()));
		martialNum = ((BasePlugin)this).Config.Bind<bool>(new ConfigDefinition("Settings", "MartialNum"), true, new ConfigDescription("武学数量无上限", (AcceptableValueBase)null, Array.Empty<object>()));
		try
		{
			Harmony val = new Harmony("com.haxx.EnhanceGameplay");
			MethodInfo methodInfo = AccessTools.Method(typeof(CanvasScaler), "Handle", (Type[])null, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(Bootstrapper), "Update", (Type[])null, (Type[])null);
			val.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(methodInfo2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			val.PatchAll(typeof(MiscPatch));
			if (toolUnbreakable.Value)
			{
				val.PatchAll(typeof(ToolPatch));
			}
			if (tradeFreely.Value)
			{
				val.PatchAll(typeof(InteractionPatch));
			}
			if (martialNum.Value)
			{
				val.PatchAll(typeof(MartialNumPatch));
			}
			if (easyQTE.Value)
			{
				val.PatchAll(typeof(EasyQTEPatch));
			}
		}
		catch
		{
			log.LogError((object)"FAILED to Apply Hooks's!");
		}
		log.LogMessage((object)"Initializing Il2CppTypeSupport...");
		Il2CppTypeSupport.Initialize();
		Bootstrapper.Create("BootStrapperGO");
	}
}
