using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib;
using WuLin;
using WuLin.GameFrameworks;
using Object = UnityEngine.Object;

namespace EnhanceGameplay;

public class MartialNumPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(GameCharacterInstance), "CouldLearnKungfu")]
	public static void CouldLearnKungfu_PostPatch(ref GameCharacterInstance.EquipingCheckResult __result)
	{
		if ((int)__result == 4)
		{
			__result = (GameCharacterInstance.EquipingCheckResult)0;
		}
	}

	[HarmonyPatch(typeof(UIKongfuPanel), "InitLeftPanel")]
	[HarmonyPostfix]
	public static void InitLeftPanel_PostPatch(UIKongfuPanel __instance)
	{
		if (__instance == null || IsUnityNull((Object)(object)__instance.LearnedSkillPanel))
		{
			return;
		}
		ModComponent.TryInitializeMartialPanel();
		Transform learnedSkillPanel = __instance.LearnedSkillPanel;
		if (learnedSkillPanel.childCount > 9)
		{
			((Component)learnedSkillPanel.GetChild(9)).gameObject.SetActive(true);
		}
		foreach (GameObject item in (Il2CppArrayBase<GameObject>)(object)GTTools.Children(((Component)learnedSkillPanel).gameObject))
		{
			if (item == null)
			{
				continue;
			}
			TryBindMoveForwardButton(__instance, item.transform);
		}
		ModComponent.RefreshLearnedSkillScrollArea(learnedSkillPanel);
	}

	private static void TryBindMoveForwardButton(UIKongfuPanel owner, Transform item)
	{
		if (owner == null || IsUnityNull((Object)(object)item))
		{
			return;
		}
		Transform upButton = item.Find("MoveForward");
		if (IsUnityNull((Object)(object)upButton))
		{
			return;
		}
		TMP_Text label = ((Component)upButton).gameObject.GetComponentInChildren<TextMeshProUGUI>();
		if (!IsUnityNull((Object)(object)label))
		{
			label.text = "前置";
		}
		Button button = ((Component)upButton).GetComponent<Button>();
		UILearnedSkillPanel learnedSkillPanel = ((Component)item).GetComponent<UILearnedSkillPanel>();
		KungfuInstance data = IsUnityNull((Object)(object)learnedSkillPanel) ? null : learnedSkillPanel.data;
		bool hasData = data != null;
		((Component)upButton).gameObject.SetActive(hasData);
		if (IsUnityNull((Object)(object)button) || !hasData)
		{
			return;
		}
		((UnityEventBase)button.onClick).RemoveAllListeners();
		((UnityEvent)(object)button.onClick).AddListener(delegate
		{
			MoveKungfuToFront(owner, learnedSkillPanel);
		});
	}

	private static void MoveKungfuToFront(UIKongfuPanel owner, UILearnedSkillPanel learnedSkillPanel)
	{
		if (owner == null || IsUnityNull((Object)(object)learnedSkillPanel))
		{
			return;
		}
		Transform parent = ((Component)learnedSkillPanel).transform;
		if (IsUnityNull((Object)(object)parent) || parent.GetSiblingIndex() == 0)
		{
			return;
		}
		KungfuInstance data = learnedSkillPanel.data;
		if (data == null || data.GameCharacterInstance == null)
		{
			return;
		}
		List<KungfuInstance> kungfuInstances = data.GameCharacterInstance.KungfuInstances;
		if (kungfuInstances == null)
		{
			return;
		}
		int oldIndex = -1;
		for (int i = 0; i < kungfuInstances.Count; i++)
		{
			if (kungfuInstances[i] == data)
			{
				oldIndex = i;
				break;
			}
		}
		if (oldIndex <= 0)
		{
			return;
		}
		kungfuInstances.Remove(data);
		kungfuInstances.Insert(0, data);
		owner.ClearKongfu();
		owner.InitLeftPanel();
	}

	private static bool IsUnityNull(Object unityObject)
	{
		return (object)unityObject == null || unityObject == (Object)null;
	}
}
