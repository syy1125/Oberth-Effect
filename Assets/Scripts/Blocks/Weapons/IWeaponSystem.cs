using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public interface IWeaponSystemRegistry : IBlockRegistry<IWeaponSystem>
{}

public interface IWeaponSystem
{
	Transform transform { get; }
	public WeaponBindingGroup WeaponBinding { get; }

	void SetPointDefenseTargetList(IReadOnlyList<PointDefenseTarget> targets);
	void SetAimPoint(Vector2? aimPoint);
	void SetFiring(bool firing);

	float GetMaxRange();
	void GetMaxFirepower(IList<FirepowerEntry> entries);
}
}