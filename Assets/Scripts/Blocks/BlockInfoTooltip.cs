using System.Text;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
/// <summary>
/// Contains static information about the block
/// </summary>
public class BlockInfoTooltip : MonoBehaviour, ITooltipComponent
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
		_bounds = new(spec.Construction.BoundsMin, spec.Construction.BoundsMax);
		_mass = spec.Physics.Mass;
	}

	public bool GetTooltip(StringBuilder builder, string indent)
	{
		string width = PhysicsUnitUtils.FormatLength(_bounds.Size.x, "F0");
		string height = PhysicsUnitUtils.FormatLength(_bounds.Size.y, "F0");

		builder
			.AppendLine($"{indent}{_fullName}")
			.AppendLine($"{indent}  <color=lime>{_cost} cost</color>")
			.AppendLine($"{indent}  {PhysicsUnitUtils.FormatMass(_mass)} mass, {width} x {height}")
			.AppendLine(
				$"{indent}  <color=red>{_maxHealth} health</color>, <color=lightblue>{_armorValue} armor</color>"
			);

		return true;
	}
}
}