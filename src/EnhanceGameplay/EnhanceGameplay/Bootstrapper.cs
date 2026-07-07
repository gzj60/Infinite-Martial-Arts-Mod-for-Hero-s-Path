using System;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnhanceGameplay;

public class Bootstrapper : MonoBehaviour
{
	private static GameObject trainer;

	internal static GameObject Create(string name)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		GameObject val = new GameObject(name);
		Object.DontDestroyOnLoad((Object)(object)val);
		Bootstrapper bootstrapper = new Bootstrapper(((Il2CppObjectBase)val.AddComponent(Il2CppType.Of<Bootstrapper>())).Pointer);
		return val;
	}

	public Bootstrapper(IntPtr intPtr)
		: base(intPtr)
	{
	}

	public void Awake()
	{
	}

	[HarmonyPostfix]
	public static void Update()
	{
		if (!((Object)(object)trainer == (Object)null))
		{
			return;
		}
		try
		{
			trainer = ModComponent.Create("ModEnhanceGameplayComponent");
			if ((Object)(object)trainer != (Object)null)
			{
				BepInExLoader.log.LogMessage((object)"EnhanceGameplay Bootstrapped!");
				BepInExLoader.log.LogMessage((object)" ");
			}
		}
		catch (Exception ex)
		{
			BepInExLoader.log.LogMessage((object)("ERROR Bootstrapping EnhanceGameplay: " + ex.Message));
			BepInExLoader.log.LogMessage((object)" ");
		}
	}
}
