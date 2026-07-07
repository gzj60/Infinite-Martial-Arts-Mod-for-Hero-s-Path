using System;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniverseLib;
using WuLin;
using WuLin.GameFrameworks;
using Object = UnityEngine.Object;

namespace EnhanceGameplay;

public class ModComponent : MonoBehaviour
{
	public static GameObject obj;

	public static ModComponent instance;

	private static bool battleSpeedInitialized;

	private static bool martialPanelInitialized;

	public static bool isBatch;

	public static bool playerSpeedUp;

	public ManualLogSource log => BepInExLoader.log;

	internal static GameObject Create(string name)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		obj = new GameObject(name);
		Object.DontDestroyOnLoad((Object)(object)obj);
		ModComponent modComponent = new ModComponent(((Il2CppObjectBase)obj.AddComponent(Il2CppType.Of<ModComponent>())).Pointer);
		return obj;
	}

	public ModComponent(IntPtr ptr)
		: base(ptr)
	{
		instance = this;
	}

	private void TryInitializeBattleSpeedButton()
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		if (battleSpeedInitialized)
		{
			return;
		}
		GameObject val = GameObject.Find("GameSingletonRoot/WuLin.UIRoot(Clone)/SafeArea/Normal/RoamingUI(Clone)/TimePanelGroup");
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		BattleUI obj = UiSingletonPrefab<BattleUI>.Instance;
		Transform val2 = ((obj != null) ? ((Component)obj).transform.Find("BattleControlGroup/SpeedButton") : null);
		if ((Object)(object)val2 == (Object)null)
		{
			return;
		}
		GameObject button = ((Component)Object.Instantiate<Transform>(val2, val.transform, false)).gameObject;
		Object.DestroyImmediate((Object)(object)button.GetComponent<Button>());
		Object.DestroyImmediate((Object)(object)button.GetComponent<EventTriggerDelegate>());
		Object.DestroyImmediate((Object)(object)button.GetComponent<HotKeyBinder>());
		((Transform)button.GetComponent<RectTransform>()).localPosition = new Vector3(-205f, 85f, 0f);
		Button val3 = button.AddComponent<Button>();
		((UnityEvent)(object)val3.onClick).AddListener(delegate
		{
			playerSpeedUp = !playerSpeedUp;
			((TMP_Text)button.gameObject.GetComponentInChildren<TextMeshProUGUI>()).text = (playerSpeedUp ? "x2" : "x1");
		});
		battleSpeedInitialized = true;
		log.LogMessage((object)"EnhanceGameplay battle speed button initialized.");
	}

	internal static void TryInitializeMartialPanel()
	{
		if (martialPanelInitialized)
		{
			return;
		}
		UIMenuPanel menuPanel = UiSingletonPrefab<UIMenuPanel>.Instance;
		if (menuPanel == null || menuPanel.panel == null)
		{
			return;
		}
		Il2CppArrayBase<GameObject> panels = (Il2CppArrayBase<GameObject>)(object)menuPanel.panel;
		if (panels.Length <= 2)
		{
			return;
		}
		GameObject martialPanel = panels[2];
		if (martialPanel == null || IsUnityNull((Object)(object)martialPanel))
		{
			return;
		}
		Transform transform = martialPanel.transform;
		if (IsUnityNull((Object)(object)transform))
		{
			return;
		}
		Transform val4 = transform.Find("MartialArts/KongFu/KongFu");
		Transform val5 = transform.Find("MartialArts/UniqueSkill/ScrollView");
		if (IsUnityNull((Object)(object)val4) || IsUnityNull((Object)(object)val5))
		{
			return;
		}
		RectTransform val4Rect = ((Component)val4).GetComponent<RectTransform>();
		ScrollRect existingScrollRect = ((Component)val4).GetComponentInParent<ScrollRect>();
		if (!IsUnityNull((Object)(object)existingScrollRect) && existingScrollRect.content == val4Rect)
		{
			SetupKungfuScrollRect(existingScrollRect, val4);
			EnsureMoveForwardButtons(val4);
			martialPanelInitialized = true;
			BepInExLoader.log.LogMessage((object)"EnhanceGameplay martial UI initialized from existing ScrollRect.");
			return;
		}
		ScrollRect component = ((Component)Object.Instantiate<Transform>(val5, val4.parent, false)).GetComponent<ScrollRect>();
		if (IsUnityNull((Object)(object)component) || IsUnityNull((Object)(object)component.content))
		{
			return;
		}
		((Object)((Component)component).gameObject).name = "EnhanceGameplayKungfuScrollView";
		RectTransform content = component.content;
		val4.SetParent(((Transform)content).parent, false);
		Object.DestroyImmediate((Object)(object)((Component)content).gameObject);
		((Component)component).GetComponent<RectTransform>().sizeDelta = new Vector2(865.7728f, 540.16f);
		((Component)component).transform.localPosition = new Vector3(0f, -275f, 0f);
		SetupKungfuScrollRect(component, val4);
		EnsureMoveForwardButtons(val4);
		martialPanelInitialized = true;
		BepInExLoader.log.LogMessage((object)"EnhanceGameplay martial UI initialized.");
	}

	private static void EnsureMoveForwardButtons(Transform learnedSkillPanel)
	{
		if (BepInExLoader.martialNum != null && !BepInExLoader.martialNum.Value)
		{
			return;
		}
		foreach (GameObject item in (Il2CppArrayBase<GameObject>)(object)GTTools.Children(((Component)learnedSkillPanel).gameObject))
		{
			if (item == null)
			{
				continue;
			}
			Transform removeButton = item.transform.Find("Remove");
			if (IsUnityNull((Object)(object)removeButton) || !IsUnityNull((Object)(object)item.transform.Find("MoveForward")))
			{
				continue;
			}
			Transform moveForwardButton = Object.Instantiate<Transform>(removeButton, item.transform, false);
			((Object)moveForwardButton).name = "MoveForward";
			removeButton.localPosition = new Vector3(140f, -18f, 0f);
			moveForwardButton.localPosition = new Vector3(45f, -18f, 0f);
			TMP_Text label = ((Component)moveForwardButton).gameObject.GetComponentInChildren<TextMeshProUGUI>();
			if (!IsUnityNull((Object)(object)label))
			{
				label.text = "前置";
			}
		}
	}

	private static void SetupKungfuScrollRect(ScrollRect scrollRect, Transform learnedSkillPanel)
	{
		if (IsUnityNull((Object)(object)scrollRect) || IsUnityNull((Object)(object)learnedSkillPanel))
		{
			return;
		}
		RectTransform content = ((Component)learnedSkillPanel).GetComponent<RectTransform>();
		if (IsUnityNull((Object)(object)content))
		{
			return;
		}
		RectTransform viewport = scrollRect.viewport;
		if (IsUnityNull((Object)(object)viewport))
		{
			Transform viewportTransform = ((Component)scrollRect).transform.Find("Viewport");
			if (!IsUnityNull((Object)(object)viewportTransform))
			{
				viewport = ((Component)viewportTransform).GetComponent<RectTransform>();
			}
		}
		if (!IsUnityNull((Object)(object)viewport))
		{
			scrollRect.viewport = viewport;
		}
		ContentSizeFitter fitter = ((Component)content).GetComponent<ContentSizeFitter>();
		if (!IsUnityNull((Object)(object)fitter))
		{
			Object.DestroyImmediate((Object)(object)fitter);
		}
		scrollRect.content = content;
		scrollRect.vertical = true;
		scrollRect.horizontal = false;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.inertia = true;
		scrollRect.scrollSensitivity = Mathf.Max(scrollRect.scrollSensitivity, 30f);
		content.anchorMin = new Vector2(content.anchorMin.x, 1f);
		content.anchorMax = new Vector2(content.anchorMax.x, 1f);
		content.pivot = new Vector2(content.pivot.x, 1f);
		content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
		RefreshLearnedSkillScrollArea(learnedSkillPanel);
	}

	internal static void RefreshLearnedSkillScrollArea(Transform learnedSkillPanel)
	{
		if (IsUnityNull((Object)(object)learnedSkillPanel))
		{
			return;
		}
		RectTransform content = ((Component)learnedSkillPanel).GetComponent<RectTransform>();
		if (IsUnityNull((Object)(object)content))
		{
			return;
		}
		ScrollRect scrollRect = ((Component)learnedSkillPanel).GetComponentInParent<ScrollRect>();
		RectTransform viewport = null;
		if (!IsUnityNull((Object)(object)scrollRect))
		{
			viewport = scrollRect.viewport;
		}
		if (IsUnityNull((Object)(object)viewport) && !IsUnityNull((Object)(object)learnedSkillPanel.parent))
		{
			viewport = ((Component)learnedSkillPanel.parent).GetComponent<RectTransform>();
		}
		float viewportHeight = IsUnityNull((Object)(object)viewport) ? 0f : viewport.rect.height;
		bool hasChildBounds = false;
		float top = 0f;
		float bottom = 0f;
		for (int i = 0; i < learnedSkillPanel.childCount; i++)
		{
			Transform child = learnedSkillPanel.GetChild(i);
			if (IsUnityNull((Object)(object)child) || !((Component)child).gameObject.activeSelf)
			{
				continue;
			}
			RectTransform childRect = ((Component)child).GetComponent<RectTransform>();
			if (IsUnityNull((Object)(object)childRect))
			{
				continue;
			}
			Rect rect = childRect.rect;
			float childTop = childRect.anchoredPosition.y + (1f - childRect.pivot.y) * rect.height;
			float childBottom = childRect.anchoredPosition.y - childRect.pivot.y * rect.height;
			if (!hasChildBounds)
			{
				top = childTop;
				bottom = childBottom;
				hasChildBounds = true;
				continue;
			}
			top = Mathf.Max(top, childTop);
			bottom = Mathf.Min(bottom, childBottom);
		}
		if (!hasChildBounds)
		{
			return;
		}
		float requiredHeight = Mathf.Max(viewportHeight, top - bottom + 24f);
		content.sizeDelta = new Vector2(content.sizeDelta.x, requiredHeight);
		if (!IsUnityNull((Object)(object)scrollRect))
		{
			scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
		Canvas.ForceUpdateCanvases();
	}

	private static bool IsUnityNull(Object unityObject)
	{
		return (object)unityObject == null || unityObject == (Object)null;
	}

	public void Update()
	{
		if (!battleSpeedInitialized)
		{
			TryInitializeBattleSpeedButton();
		}
		if (!martialPanelInitialized && (BepInExLoader.martialNum == null || BepInExLoader.martialNum.Value))
		{
			TryInitializeMartialPanel();
		}
	}
}
