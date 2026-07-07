using System;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EnhanceGameplay.UI;

public class WindowDragHandler : MonoBehaviour
{
	public static WindowDragHandler instance;

	private const int NON_EXISTING_TOUCH = -98456;

	private static RectTransform rectTransform;

	private static int pointerId = -98456;

	private static Vector2 initialTouchPos;

	public WindowDragHandler(IntPtr ptr)
		: base(ptr)
	{
		instance = this;
	}

	public void Awake()
	{
		BepInExLoader.log.LogMessage((object)"      WindowsDragHandler Awake()");
		rectTransform = ((Component)this).gameObject.GetComponent<RectTransform>();
	}

	[HarmonyPostfix]
	public static void OnBeginDrag(PointerEventData eventData)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		BepInExLoader.log.LogMessage((object)"WindowsDragHandler OnBeginDrag()");
		if (pointerId != -98456)
		{
			eventData.pointerDrag = null;
			return;
		}
		pointerId = eventData.pointerId;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, Camera.current, out initialTouchPos);
		ManualLogSource log = BepInExLoader.log;
		bool flag = default(bool);
		BepInExMessageLogInterpolatedStringHandler val = new BepInExMessageLogInterpolatedStringHandler(33, 1, out flag);
		if (flag)
		{
			((BepInExLogInterpolatedStringHandler)val).AppendLiteral("WindowsDragHandler OnBeginDrag() ");
			((BepInExLogInterpolatedStringHandler)val).AppendFormatted<Vector2>(initialTouchPos);
		}
		log.LogMessage(val);
	}

	[HarmonyPostfix]
	public static void OnDrag(PointerEventData eventData)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		BepInExLoader.log.LogMessage((object)"WindowsDragHandler OnDrag()");
		if (eventData.pointerId == pointerId)
		{
			BepInExLoader.log.LogMessage((object)"WindowsDragHandler OnDrag() eventData is null");
			Vector2 val = default(Vector2);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, Camera.current, out val);
			BepInExLoader.log.LogMessage((object)"WindowsDragHandler OnDrag() rectTransform is null");
			Vector2 val2 = val - initialTouchPos;
			ManualLogSource log = BepInExLoader.log;
			bool flag = default(bool);
			BepInExMessageLogInterpolatedStringHandler val3 = new BepInExMessageLogInterpolatedStringHandler(33, 1, out flag);
			if (flag)
			{
				((BepInExLogInterpolatedStringHandler)val3).AppendLiteral("WindowsDragHandler OnBeginDrag() ");
				((BepInExLogInterpolatedStringHandler)val3).AppendFormatted<Vector2>(initialTouchPos);
			}
			log.LogMessage(val3);
			Transform transform = ((Component)rectTransform).gameObject.transform;
			transform.position += new Vector3(val2.x, val2.y, Camera.current.nearClipPlane);
		}
	}

	[HarmonyPostfix]
	public static void OnEndDrag(PointerEventData eventData)
	{
		BepInExLoader.log.LogMessage((object)"WindowsDragHandler OnEndDrag()");
		if (eventData.pointerId == pointerId)
		{
			pointerId = -98456;
		}
	}
}
