using System;
using Syy1125.OberthEffect.Foundation.Physics;
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

	public Vector2 ScreenPosition { get; private set; }
	public bool OnScreen { get; private set; }

	private Camera _mainCamera;
	private RectTransform _rectTransform;
	private RectTransform _canvasTransform;

	private void Awake()
	{
		_mainCamera = Camera.main;
		_rectTransform = GetComponent<RectTransform>();
		_canvasTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
	}

	public void Init()
	{
		LateUpdate();
	}

	private void LateUpdate()
	{
		if (Target == null) return;

		(Vector2 targetPosition, float targetSize) = GetTargetPositionAndSize();
		ScreenPosition = _mainCamera.WorldToViewportPoint(targetPosition);
		OnScreen = ScreenPosition.x >= 0 && ScreenPosition.x <= 1 && ScreenPosition.y >= 0 && ScreenPosition.y <= 1;

		if (OnScreen)
		{
			_rectTransform.anchorMin = ScreenPosition;
			_rectTransform.anchorMax = ScreenPosition;

			float apparentSize = targetSize
			                     * TargetSizeMultiplier
			                     * _canvasTransform.rect.height
			                     / _mainCamera.orthographicSize;
			_rectTransform.offsetMin = new Vector2(-apparentSize, -apparentSize);
			_rectTransform.offsetMax = new Vector2(apparentSize, apparentSize);
			_rectTransform.pivot = new Vector2(0.5f, 0.5f);

			if (Label != null)
			{
				if (ScreenPosition.y < 0.5f)
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
		}
		else
		{
			Vector2 targetDirection = ScreenPosition - new Vector2(0.5f, 0.5f);
			Vector2 edgePoint =
				targetDirection / Mathf.Max(Mathf.Abs(targetDirection.x), Mathf.Abs(targetDirection.y)) * 0.5f
				+ new Vector2(0.5f, 0.5f);

			_rectTransform.anchorMin = edgePoint;
			_rectTransform.anchorMax = edgePoint;
			_rectTransform.offsetMin = new Vector2(-OffScreenSize * edgePoint.x, -OffScreenSize * edgePoint.y);
			_rectTransform.offsetMax = new Vector2(
				OffScreenSize * (1 - edgePoint.x), OffScreenSize * (1 - edgePoint.y)
			);

			if (Label != null)
			{
				Vector2 labelAnchor = Vector2.one - edgePoint;
				Label.anchorMin = labelAnchor;
				Label.anchorMax = labelAnchor;
				Label.pivot = edgePoint;
			}
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