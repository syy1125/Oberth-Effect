using System;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(RectTransform))]
public class HighlightTarget : MonoBehaviour
{
	public Transform Target;
	public RectTransform Label;
	public float TargetSizeMultiplier;
	public float OffScreenSize;

	private Camera _mainCamera;
	private RectTransform _rectTransform;
	private RectTransform _canvasTransform;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_rectTransform = GetComponent<RectTransform>();
		_canvasTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
	}

	private void LateUpdate()
	{
		(Vector2 targetPosition, float targetSize) = GetTargetPositionAndSize();
		Vector2 screenPosition = _mainCamera.WorldToViewportPoint(targetPosition);

		if (screenPosition.x >= 0 && screenPosition.x <= 1 && screenPosition.y >= 0 && screenPosition.y <= 1)
		{
			_rectTransform.anchorMin = screenPosition;
			_rectTransform.anchorMax = screenPosition;

			float apparentSize = targetSize
			                     * TargetSizeMultiplier
			                     * _canvasTransform.rect.height
			                     / _mainCamera.orthographicSize;
			_rectTransform.offsetMin = new Vector2(-apparentSize, -apparentSize);
			_rectTransform.offsetMax = new Vector2(apparentSize, apparentSize);
			_rectTransform.pivot = new Vector2(0.5f, 0.5f);

			if (screenPosition.y < 0.5f)
			{
				Label.anchorMin = new Vector2(0.5f, 1f);
				Label.anchorMax = new Vector2(0.5f, 1f);
				Label.pivot = new Vector2(0.5f, 0f);
			}
			else
			{
				Label.anchorMin = new Vector2(0.5f, 0f);
				Label.anchorMax = new Vector2(0.5f, 0f);
				Label.pivot = new Vector2(0.5f, 1f);
			}
		}
		else
		{
			Vector2 targetDirection = screenPosition - new Vector2(0.5f, 0.5f);
			Vector2 edgePoint =
				targetDirection / Mathf.Max(Mathf.Abs(targetDirection.x), Mathf.Abs(targetDirection.y)) * 0.5f
				+ new Vector2(0.5f, 0.5f);

			_rectTransform.anchorMin = edgePoint;
			_rectTransform.anchorMax = edgePoint;
			_rectTransform.offsetMin = new Vector2(-OffScreenSize * edgePoint.x, -OffScreenSize * edgePoint.y);
			_rectTransform.offsetMax = new Vector2(
				OffScreenSize * (1 - edgePoint.x), OffScreenSize * (1 - edgePoint.y)
			);

			Vector2 labelAnchor = Vector2.one - edgePoint;
			Label.anchorMin = labelAnchor;
			Label.anchorMax = labelAnchor;
			Label.pivot = edgePoint;
		}
	}

	private Tuple<Vector2, float> GetTargetPositionAndSize()
	{
		var blockManager = Target.GetComponent<ConstructBlockManager>();
		if (blockManager != null)
		{
			BoundsInt vehicleBounds = blockManager.GetBounds();
			Vector2 centroid = Target.TransformPoint(vehicleBounds.center);
			float size = Mathf.Max(vehicleBounds.xMax - vehicleBounds.xMin, vehicleBounds.yMax - vehicleBounds.yMin);
			return Tuple.Create(centroid, size / 2f);
		}

		var radiusProvider = Target.GetComponent<ICollisionRadiusProvider>();
		if (radiusProvider != null)
		{
			return Tuple.Create((Vector2) Target.position, radiusProvider.GetCollisionRadius());
		}

		return Tuple.Create((Vector2) Target.position, 1f);
	}
}
}