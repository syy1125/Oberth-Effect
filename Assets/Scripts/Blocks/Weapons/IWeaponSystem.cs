using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public interface IWeaponSystemRegistry : IBlockRegistry<IWeaponSystem>
{}

public interface IWeaponSystem
{
	Transform transform { get; }
	public WeaponBindingGroup WeaponBinding { get; }

	void SetAimPoint(Vector2? aimPoint);
	void SetFiring(bool firing);

	float GetMaxRange();
	IReadOnlyDictionary<DamageType, float> GetMaxFirepower();
}
}