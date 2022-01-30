using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IGuidedWeaponTarget
{
	Vector2 GetEffectivePosition();
	Vector2 GetEffectiveVelocity();

	T[] GetComponents<T>();
}
}