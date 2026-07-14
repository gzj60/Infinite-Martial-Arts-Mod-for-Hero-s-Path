using System;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using WuLin;

namespace EnhanceGameplay;

public static class InternalKungfuPatch
{
	[ThreadStatic]
	private static bool creatingAdditionalInternalEvents;

	[HarmonyPatch(typeof(BattleActor), "CreateInternalKungfuEffectEvents")]
	[HarmonyPostfix]
	public static void CreateInternalKungfuEffectEvents_Postfix(
		BattleActor __instance,
		DynamicModifier.DynamicModifierActiveStage dynamicModifierActiveStage,
		ref InternalKungfuEffectEvent __result)
	{
		if (creatingAdditionalInternalEvents || __instance == null ||
			!IsFriendlyActor(__instance) || !IsInternalKungfuBattleStage(dynamicModifierActiveStage))
		{
			return;
		}

		BattleActorCreateInfo info = __instance.info;
		GameCharacterInstance character = info == null ? null : info.characterInstance;
		if (character == null)
		{
			return;
		}

		List<KungfuInstance> internalKungfu = character.GetInternalKungku();
		if (internalKungfu == null || internalKungfu.Count == 0)
		{
			return;
		}

		BuildAdditionalEvents(__instance, character, internalKungfu, dynamicModifierActiveStage, ref __result);
	}

	private static bool IsFriendlyActor(BattleActor actor)
	{
		BattleTeamEnum team = actor.ServeBattleTeam;
		return team == BattleTeamEnum.Player || team == BattleTeamEnum.Allie;
	}

	private static bool IsInternalKungfuBattleStage(DynamicModifier.DynamicModifierActiveStage stage)
	{
		return stage switch
		{
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuEnterBattle => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuBeforeAttack => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuAfterAttack => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuBeforeHit => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuAfterHit => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuAfterAction => true,
			DynamicModifier.DynamicModifierActiveStage.BattleInternalKungfuSwitch => true,
			_ => false
		};
	}

	private static void BuildAdditionalEvents(
		BattleActor actor,
		GameCharacterInstance character,
		List<KungfuInstance> internalKungfu,
		DynamicModifier.DynamicModifierActiveStage dynamicModifierActiveStage,
		ref InternalKungfuEffectEvent result)
	{
		KungfuInstance originalActive = character.activedInternalKungfu;
		for (int i = 0; i < internalKungfu.Count; i++)
		{
			KungfuInstance kungfu = internalKungfu[i];
			if (kungfu == null || kungfu == originalActive)
			{
				continue;
			}

			try
			{
				InternalKungfuEffectEvent additional = BuildEventForKungfu(
					actor,
					character,
					kungfu,
					dynamicModifierActiveStage);
				result = AppendEventChain(result, additional);
			}
			catch (Exception ex)
			{
				BepInExLoader.log?.LogError(
					$"Failed to build internal kungfu effect for {kungfu.TempleteUid}: {ex}");
			}
		}
	}

	private static InternalKungfuEffectEvent BuildEventForKungfu(
		BattleActor actor,
		GameCharacterInstance character,
		KungfuInstance kungfu,
		DynamicModifier.DynamicModifierActiveStage dynamicModifierActiveStage)
	{
		KungfuInstance originalActive = character.activedInternalKungfu;
		int originalActiveId = character.m_activedInternalKunfuId;
		try
		{
			character.activedInternalKungfu = kungfu;
			character.m_activedInternalKunfuId = kungfu.TempleteUid;
			creatingAdditionalInternalEvents = true;
			return actor.CreateInternalKungfuEffectEvents(dynamicModifierActiveStage);
		}
		finally
		{
			creatingAdditionalInternalEvents = false;
			character.activedInternalKungfu = originalActive;
			character.m_activedInternalKunfuId = originalActiveId;
		}
	}

	private static InternalKungfuEffectEvent AppendEventChain(
		InternalKungfuEffectEvent result,
		InternalKungfuEffectEvent additional)
	{
		if (additional == null)
		{
			return result;
		}
		if (result == null)
		{
			return additional;
		}

		BattleFieldEvent tail = result.FindLast();
		BattleFieldEvent head = additional.FindFirst();
		if (tail != null && head != null)
		{
			tail.LinkWith(head);
		}
		return result;
	}
}
