using System.Collections.Generic;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Foundation.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public interface IWeaponBlockRegistry : IBlockRegistry<IWeaponBlock>
{}

public interface IWeaponBlock
{
	Transform transform { get; }
	public WeaponBindingGroup WeaponBinding { get; }

	void SetPointDefenseTargetList(IReadOnlyList<PointDefenseTargetData> targetData);
	void SetTargetPhotonId(int? targetId);
	void SetAimPoint(Vector2? aimPoint);
	void SetFiring(bool firing);

	float GetMaxRange();
	void GetMaxFirepower(IList<FirepowerEntry> entries);
}
}