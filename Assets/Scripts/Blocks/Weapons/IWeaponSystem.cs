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

	int GetOwnerId();

	void SetAimPoint(Vector2? aimPoint);
	void SetFiring(bool firing);

	Dictionary<DamageType, float> GetMaxFirepower();
	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
}
}