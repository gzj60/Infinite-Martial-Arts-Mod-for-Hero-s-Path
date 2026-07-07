using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using WuLin;

namespace EnhanceGameplay;

public class MiscPatch
{
	public static int battleSpeed = 1;

	public static ManualLogSource log => BepInExLoader.log;

	[HarmonyPatch(typeof(BattleUI), "OnSpeedButtonClickHandler")]
	[HarmonyPrefix]
	public static void BattleUISwitchSpeed_PrePatch()
	{
		battleSpeed = battleSpeed * 2 % 7;
	}

	[HarmonyPatch(typeof(BattleUI), "SwitchSpeed")]
	[HarmonyPostfix]
	public static void BattleUISwitchSpeed_PostPatch(BattleUI __instance)
	{
		((TMP_Text)__instance.speedText).text = $"x{battleSpeed}";
		Time.timeScale = battleSpeed;
	}

	[HarmonyPatch(typeof(Role), "UpdateSpeed")]
	[HarmonyPostfix]
	public static void RoleUpdateSpeed_PostPatch(Role __instance)
	{
		if (ModComponent.playerSpeedUp && (Object)(object)__instance == (Object)(object)MonoSingleton<RoamingManager>.Instance.player)
		{
			__instance.speed *= 2.0;
		}
	}
}
