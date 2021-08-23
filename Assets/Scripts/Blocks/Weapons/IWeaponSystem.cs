using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public interface IWeaponSystemRegistry : IBlockRegistry<IWeaponSystem>, IEventSystemHandler
{}

public interface IWeaponSystem
{
	Transform transform { get; }
	public WeaponBindingGroup WeaponBinding { get; }

	void SetAimPoint(Vector2? aimPoint);
	void SetFiring(bool firing);

	IReadOnlyDictionary<DamageType, float> GetMaxFirepower();
	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
}
}