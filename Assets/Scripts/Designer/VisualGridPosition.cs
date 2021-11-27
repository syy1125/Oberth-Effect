using System;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
[RequireComponent(typeof(SpriteRenderer))]
public class VisualGridPosition : MonoBehaviour
{
	private Transform _parent;
	private Camera _mainCamera;
	private SpriteRenderer _sprite;

	private void Awake()
	{
		_parent = transform.parent;
		_mainCamera = Camera.main;
		_sprite = GetComponent<SpriteRenderer>();
	}

	private void Start()
	{
		float opacity = PlayerPrefs.GetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, 0.2f);
		var color = _sprite.color;
		color.a = opacity;
		_sprite.color = color;
	}

	private void LateUpdate()
	{
		Vector2 minPoint = _parent.InverseTransformPoint(_mainCamera.ViewportToWorldPoint(Vector3.zero));
		Vector2 maxPoint = _parent.InverseTransformPoint(_mainCamera.ViewportToWorldPoint(Vector3.one));

		Vector2 center = (minPoint + maxPoint) / 2;
		Vector2 size = maxPoint - minPoint;

		int scale = 1;
		while (size.x > 50 || size.y > 50)
		{
			center /= 5;
			size /= 5;
			scale *= 5;
		}

		Vector2Int centerInt = Vector2Int.RoundToInt(center);
		Vector2Int sizeInt = Vector2Int.RoundToInt(size) + Vector2Int.one * 2;
		if (sizeInt.x % 2 == 0) sizeInt.x++;
		if (sizeInt.y % 2 == 0) sizeInt.y++;

		transform.localScale = Vector3.one * scale;
		transform.localPosition = new Vector3(centerInt.x * scale, centerInt.y * scale, transform.localPosition.z);
		_sprite.size = sizeInt;
	}
}
}