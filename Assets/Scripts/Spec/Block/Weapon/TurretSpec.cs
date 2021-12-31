using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block.Weapon
{
public struct TurretSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float RotationSpeed;
	public Vector2 TurretPivotOffset;
	public RendererSpec[] Renderers;
}
}