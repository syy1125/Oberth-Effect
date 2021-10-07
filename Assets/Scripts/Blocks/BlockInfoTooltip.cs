using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
/// <summary>
/// Contains static information about the block
/// </summary>
public class BlockInfoTooltip : MonoBehaviour, ITooltipProvider
{
	private string _fullName;
	private int _cost;

	private float _maxHealth;
	private float _armorValue;

	private BlockBounds _bounds;

	private float _mass;

	public void LoadSpec(BlockSpec spec)
	{
		_fullName = spec.Info.FullName;
		_cost = spec.Cost;

		_maxHealth = spec.Combat.MaxHealth;
		_armorValue = spec.Combat.ArmorValue;
		_bounds = new BlockBounds(spec.Construction.BoundsMin, spec.Construction.BoundsMax);
		_mass = spec.Physics.Mass;
	}

	public string GetTooltip()
	{
		float width = _bounds.Size.x * PhysicsConstants.METERS_PER_UNIT_LENGTH;
		float height = _bounds.Size.y * PhysicsConstants.METERS_PER_UNIT_LENGTH;

		return string.Join(
			"\n",
			_fullName,
			$"  Cost {_cost}",
			$"  {_mass * PhysicsConstants.KG_PER_UNIT_MASS:#,0.##}kg, {width:F0}m × {height:F0}m",
			$"  <color=\"red\">{_maxHealth} health</color>, <color=\"lightblue\">{_armorValue} armor</color>"
		).Trim();
	}
}
}