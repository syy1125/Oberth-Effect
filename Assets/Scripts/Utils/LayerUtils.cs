using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class LayerUtils
{
	public static void SetLayerRecursively(GameObject target, int layer, int ignoreMask = 0)
	{
		if ((ignoreMask & (1 << target.layer)) == 0)
		{
			target.layer = layer;
		}

		foreach (Transform child in target.transform)
		{
			SetLayerRecursively(child.gameObject, layer);
		}
	}
}
}