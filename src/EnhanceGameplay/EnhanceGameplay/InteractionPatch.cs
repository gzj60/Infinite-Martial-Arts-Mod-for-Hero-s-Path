using HarmonyLib;
using WuLin.StateMachine;

namespace EnhanceGameplay;

public class InteractionPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(StartSpInteractiveActionNode), "InvokeAction")]
	public static void StartSpInteractiveActionNode_PrePatch(StartSpInteractiveActionNode __instance)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		if ((int)__instance.interactiveType == 0)
		{
			__instance.useKey = false;
			__instance.minimalRalation = -100;
		}
	}
}
