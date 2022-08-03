using System;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
[Serializable]
[CreateSchemaFile("ParticleSystemSpecSchema")]
public struct ParticleSystemSpec
{
	public Vector2 Offset;
	public Vector2 Direction;
	[SchemaDescription("Half-angle of the particle spread.")]
	public float SpreadAngle;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Size;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxSpeed;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float Lifetime;
	[ValidateColor(true)]
	[SchemaDescription(
		"The color of the particles. Can take either an accepted HTML color string (\"red\") or a hex code (\"#ff0000\"). Depending on the context, this could also reference a color in the paint scheme of the vehicle (\"primary\")."
	)]
	public string Color;

	public float EmissionRateOverTime;
	public float EmissionRateOverDistance;
}
}