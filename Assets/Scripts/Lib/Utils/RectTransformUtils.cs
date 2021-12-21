using UnityEngine;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class RectTransformUtils
{
	public static bool ScreenPointToNormalizedPointInRectangle(
		RectTransform transform, Vector2 screenPoint, Camera camera, out Vector2 rectPoint
	)
	{
		bool hit = RectTransformUtility.ScreenPointToLocalPointInRectangle(
			transform, screenPoint, camera, out Vector2 localPoint
		);
		var rect = transform.rect;
		rectPoint = (localPoint - rect.min) / rect.size;
		return hit;
	}
}
}