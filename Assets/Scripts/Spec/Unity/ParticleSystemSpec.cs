using System;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
[Serializable]
public struct ParticleSystemSpec
{
	public Vector2 Offset;
	public Vector2 Direction;
	public float SpreadAngle;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Size;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxSpeed;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Lifetime;
	[ValidateColor(true)]
	public string Color;

	public float EmissionRateOverTime;
	public float EmissionRateOverDistance;
}
}