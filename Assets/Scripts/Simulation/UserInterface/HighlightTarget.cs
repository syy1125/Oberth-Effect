using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(RectTransform))]
public class HighlightTarget : MonoBehaviour
{
	public Transform Target;
	public RectTransform Highlight;
	public RectTransform Label;
	public float TargetSize;
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

	private void Update()
	{
		Vector2 screenPosition = _mainCamera.WorldToViewportPoint(Target.position);

		if (screenPosition.x >= 0 && screenPosition.x <= 1 && screenPosition.y >= 0 && screenPosition.y <= 1)
		{
			_rectTransform.anchorMin = screenPosition;
			_rectTransform.anchorMax = screenPosition;

			float apparentSize = TargetSize * _canvasTransform.rect.height / _mainCamera.orthographicSize;
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
}
}