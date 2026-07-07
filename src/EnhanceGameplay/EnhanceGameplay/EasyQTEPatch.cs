using HarmonyLib;
using WuLin;

namespace EnhanceGameplay;

public class EasyQTEPatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UISlider), "InitPanel")]
	public static void UISliderInitPanel_PrePatch(UISlider __instance, UISlider.SliderParameter par)
	{
		par.Range = 1f;
	}
}
