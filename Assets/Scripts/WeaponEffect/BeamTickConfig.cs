using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public struct BeamTickConfig
{
	public float DamagePerTick;
	public DamageType DamageType;
	public float ArmorPierce; // Note that explosive damage will always have damage output of value 1
	public float ExplosionRadius; // Only relevant for explosive damage

	public Vector3 Origin;
	public Vector3 Direction;
	public float MaxRange;
}
}