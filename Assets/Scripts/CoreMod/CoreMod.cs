using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod
{
public static class CoreMod
{
	public static void Init()
	{
		Debug.Log("Hello world!");
		BlockSpec.Register<VolatileSpec, VolatileBlock>("Volatile");
	}
}
}