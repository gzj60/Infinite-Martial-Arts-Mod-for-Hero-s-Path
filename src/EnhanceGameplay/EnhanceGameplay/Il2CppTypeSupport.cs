using System;
using System.Reflection;

namespace EnhanceGameplay;

public class Il2CppTypeSupport
{
	private static Assembly Il2CppInteropRuntimeLib;

	private static Assembly Il2CppInteropRunCommonLib;

	private static Type Il2CppObjectBaseType;

	internal static Type Il2CppMethodInfoType;

	internal static MethodInfo Il2CppObjectBaseToPtrMethod;

	internal static MethodInfo Il2CppStringToManagedMethod;

	internal static MethodInfo ManagedStringToIl2CppMethod;

	internal static MethodInfo GetIl2CppMethodInfoPointerFieldForGeneratedMethod;

	internal static void Initialize()
	{
		Il2CppInteropRuntimeLib = Assembly.Load("Il2CppInterop.Runtime");
		Il2CppInteropRunCommonLib = Assembly.Load("Il2CppInterop.Common");
		Il2CppObjectBaseType = Il2CppInteropRuntimeLib.GetType("Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase");
		Il2CppMethodInfoType = Il2CppInteropRuntimeLib.GetType("UnhollowerBaseLib.Runtime.Runtime.Il2CppMethodInfo");
		Il2CppObjectBaseToPtrMethod = Il2CppInteropRuntimeLib.GetType("Il2CppInterop.Runtime.IL2CPP").GetMethod("Il2CppObjectBaseToPtr");
		Il2CppStringToManagedMethod = Il2CppInteropRuntimeLib.GetType("Il2CppInterop.Runtime.IL2CPP").GetMethod("Il2CppStringToManaged");
		ManagedStringToIl2CppMethod = Il2CppInteropRuntimeLib.GetType("Il2CppInterop.Runtime.IL2CPP").GetMethod("ManagedStringToIl2Cpp");
		GetIl2CppMethodInfoPointerFieldForGeneratedMethod = Il2CppInteropRunCommonLib.GetType("Il2CppInterop.Common.Il2CppInteropUtils").GetMethod("GetIl2CppMethodInfoPointerFieldForGeneratedMethod");
	}

	public static bool IsGeneratedAssemblyType(Type type)
	{
		return !Il2CppInteropRuntimeLib.Equals(null) && !Il2CppObjectBaseType.Equals(null) && !type.Equals(null) && type.IsSubclassOf(Il2CppObjectBaseType);
	}

	public static IntPtr MethodBaseToIl2CppMethodInfoPointer(MethodBase method)
	{
		FieldInfo fieldInfo = (FieldInfo)GetIl2CppMethodInfoPointerFieldForGeneratedMethod.Invoke(null, new object[1] { method });
		if (fieldInfo == null)
		{
			throw new NotSupportedException("Cannot get IntPtr for " + method.Name + " as there is no corresponding IL2CPP method");
		}
		return (IntPtr)fieldInfo.GetValue(null);
	}

	public static T Il2CppObjectPtrToIl2CppObject<T>(IntPtr ptr)
	{
		if (ptr == IntPtr.Zero)
		{
			throw new NullReferenceException("The ptr cannot be IntPtr.Zero.");
		}
		if (!IsGeneratedAssemblyType(typeof(T)))
		{
			throw new NullReferenceException("The type must be a Generated Assembly Type.");
		}
		return (T)typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[1] { typeof(IntPtr) }, new ParameterModifier[0]).Invoke(new object[1] { ptr });
	}
}
