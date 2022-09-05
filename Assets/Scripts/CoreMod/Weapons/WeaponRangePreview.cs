using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
[RequireComponent(typeof(LineRenderer))]
public class WeaponRangePreview : MonoBehaviour
{
	private const int SEGMENT_COUNT = 48;

	private Camera _mainCamera;

	private LineRenderer _line;
	private LineRenderer Line => _line != null ? _line : _line = GetComponent<LineRenderer>();

	private float _radius;

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	public void SetRadius(float radius)
	{
		_radius = radius;

		var positions = new Vector3[SEGMENT_COUNT + 1];
		for (int i = 0; i <= SEGMENT_COUNT; i++)
		{
			float angle = Mathf.PI * 2 * i / SEGMENT_COUNT;
			positions[i] = new(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f);
		}

		Line.sharedMaterial = TextureDatabase.Instance.DefaultLineMaterial;
		Line.widthCurve = AnimationCurve.Constant(0f, 1f, 0.05f);
		Line.useWorldSpace = false;
		Gradient gradient = new();
		gradient.SetKeys(
			new GradientColorKey[]
			{
				new() { time = 0f, color = Color.white },
				new() { time = 1f, color = Color.white }
			},
			new GradientAlphaKey[]
			{
				new() { time = 0f, alpha = 0.5f },
				new() { time = 1f, alpha = 0.5f }
			}
		);
		Line.colorGradient = gradient;

		Line.positionCount = positions.Length;
		Line.SetPositions(positions);
	}
}
}