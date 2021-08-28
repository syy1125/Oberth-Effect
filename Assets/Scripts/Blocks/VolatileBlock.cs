using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class VolatileBlock : MonoBehaviour, IBlockDestructionEffect, ITooltipProvider
{
	private bool _alwaysExplode;
	private Vector2 _explosionOffset;
	private float _constantRadius;
	private float _constantDamage;

	public void LoadSpec(VolatileSpec spec)
	{
		_alwaysExplode = spec.AlwaysExplode;
		_explosionOffset = spec.ExplosionOffset;
		_constantRadius = spec.ConstantRadius;
		_constantDamage = spec.ConstantDamage;
	}

	public void OnDestroyedByDamage()
	{
		Debug.Log($"Block \"{gameObject}\" is exploding for {_constantDamage} damage.");
		ExplosionManager.Instance.CreateExplosionAt(
			transform.TransformPoint(_explosionOffset), _constantRadius, _constantDamage
		);
	}

	public string GetTooltip()
	{
		return _alwaysExplode
			? $"<color=\"red\">Volatile</color>: Explodes for {_constantDamage:F0} damage in a {_constantRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m radius when destroyed."
			: $"<color=\"orange\">Sometimes volatile</color>: Can explode for up to {_constantDamage:F0} damage in a {_constantRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m radius when destroyed.";
	}
}
}