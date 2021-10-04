using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public static class ComponentUtils
{
	public static T GetBehaviourInParent<T>(Transform target)
	{
		while (target != null)
		{
			foreach (MonoBehaviour behaviour in target.GetComponents<MonoBehaviour>())
			{
				if (behaviour is T t)
				{
					return t;
				}
			}

			target = target.parent;
		}

		return default;
	}
}
}