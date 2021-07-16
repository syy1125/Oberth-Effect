using System;
using Syy1125.OberthEffect.Common.ColorScheme;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
[RequireComponent(typeof(LineRenderer))]
public class BeanWeaponLineEffect : MonoBehaviour, IBeamWeaponVisualEffect
{
	private LineRenderer _line;

	private void Awake()
	{
		_line = GetComponent<LineRenderer>();
	}

	public void SetColorScheme(ColorScheme colors)
	{
		_line.startColor = colors.PrimaryColor;
		_line.endColor = colors.PrimaryColor;
	}

	public void SetBeamPoints(Vector3 begin, Vector3 end, bool hit)
	{
		_line.SetPositions(new[] { begin, end });
	}
}
}