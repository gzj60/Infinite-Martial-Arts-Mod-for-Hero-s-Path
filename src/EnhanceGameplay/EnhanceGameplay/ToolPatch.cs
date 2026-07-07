using System;
using HarmonyLib;
using UnityEngine;
using WuLin;

namespace EnhanceGameplay;

public class ToolPatch
{
	public static bool isMinning;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MiningBatchUI), "OnPlayEnd")]
	public static void MiningBatchUIOnPlayEnd_PrePatch(MiningBatchUI __instance)
	{
		__instance.tableData.Level = 0;
		isMinning = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MiningBatchUI), "OnPlayEnd")]
	public static void MiningBatchUIOnPlayEnd_PostPatch(MiningBatchUI __instance)
	{
		isMinning = false;
	}

	[HarmonyPatch(typeof(UnityEngine.Random), "Range", new Type[]
	{
		typeof(float),
		typeof(float)
	})]
	[HarmonyPostfix]
	public static void RandomRange_PostPatch(ref float __result)
	{
		if (isMinning && __result < 0.1f)
		{
			__result = 0.1f;
		}
	}
}
