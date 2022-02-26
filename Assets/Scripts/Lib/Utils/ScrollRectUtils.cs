using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lib.Utils
{
public static class ScrollRectUtils
{
	public static void DragEdgeScroll(ScrollRect scrollRect, PointerEventData eventData)
	{
		float viewportHeight = scrollRect.viewport.rect.height;
		RectTransformUtils.ScreenPointToNormalizedPointInRectangle(
			scrollRect.viewport, eventData.position, eventData.pressEventCamera, out Vector2 contentPoint
		);

		if (contentPoint.y > 0.95f)
		{
			scrollRect.velocity = new Vector2(
				0f, MathUtils.Remap(contentPoint.y, 0.95f, 1f, -0.5f, -0.1f) * viewportHeight
			);
		}
		else if (contentPoint.y < 0.05f)
		{
			scrollRect.velocity = new Vector2(0f, MathUtils.Remap(contentPoint.y, 0f, 0.05f, 0.1f, 0.5f) * viewportHeight);
		}
		else
		{
			scrollRect.velocity = Vector2.zero;
		}
	}
}
}