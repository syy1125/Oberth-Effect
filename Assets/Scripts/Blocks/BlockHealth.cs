using System;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
/// <remarks>
/// This interface is used by block scripts to execute effects on its destruction
/// </remarks>
internal interface IBlockDestructionEffect : IEventSystemHandler
{
	void OnDestroyedByDamage();
}

/// <remarks>
/// This interface is implemented by vehicle-level scripts to monitor block events
/// </remarks>
public interface IBlockLifecycleListener : IEventSystemHandler
{
	void OnBlockDestroyedByDamage(BlockCore blockCore);
}

public class BlockHealth : MonoBehaviour, IDamageable
{
	private BlockCore _core;
	private OwnerContext _ownerContext;

	public bool IsMine => _core.IsMine;
	public int OwnerId => _ownerContext.OwnerId;

	private float _maxHealth;
	private float _armor;

	private float _health;
	public float HealthFraction => Mathf.Clamp01(_health / _maxHealth);
	public bool IsDamaged => _maxHealth - _health > Mathf.Epsilon;

	private Vector2 _boundsMin;
	private Vector2 _boundsMax;

	private void Awake()
	{
		_core = GetComponent<BlockCore>();
		_ownerContext = GetComponentInParent<OwnerContext>();
	}

	public void LoadSpec(BlockSpec spec)
	{
		_maxHealth = spec.Combat.MaxHealth;
		_armor = spec.Combat.ArmorValue;
		_boundsMin = spec.Construction.BoundsMin;
		_boundsMax = spec.Construction.BoundsMax;
	}

	private void Start()
	{
		_health = _maxHealth;
	}

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(_boundsMin, _boundsMax);
	}

	public void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted)
	{
		float damageModifier = Mathf.Min(armorPierce / _armor, 1f);
		Debug.Assert(damageModifier > Mathf.Epsilon, "Damage modifier should not be zero");

		float effectiveDamage = damage * damageModifier;
		float effectiveHealth = _health / damageModifier;

		if (effectiveDamage < _health)
		{
			_health -= effectiveDamage;
			damage = 0f;
			damageExhausted = true;
		}
		else
		{
			_health = 0f;
			damage -= effectiveHealth;
			damageExhausted = false;

			ExecuteEvents.Execute<IBlockDestructionEffect>(
				gameObject, null, (listener, _) => listener.OnDestroyedByDamage()
			);
			ExecuteEvents.ExecuteHierarchy<IBlockLifecycleListener>(
				gameObject, null, (listener, _) => listener.OnBlockDestroyedByDamage(_core)
			);
			// Note that, for multiplayer synchronization reasons, disabling of game object will be executed by VehicleCore
		}
	}
}
}