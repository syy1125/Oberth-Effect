using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
public struct ParticleSystemSpec
{
	public Vector2 Offset;
	public Vector2 Direction;
	public float SpreadAngle;

	public float Size;
	public float MaxSpeed;
	public float Lifetime;
	public string Color;

	public float EmissionRateOverTime;
	public float EmissionRateOverDistance;
}
}