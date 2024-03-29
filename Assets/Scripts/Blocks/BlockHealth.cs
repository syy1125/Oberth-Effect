﻿using System;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
/// <remarks>
/// This interface is used by block scripts to execute effects on its destruction
/// </remarks>
public interface IBlockDestructionEffect : IEventSystemHandler
{
	void OnDestroyedByDamage();
}

/// <remarks>
/// This interface is implemented by vehicle-level scripts to monitor block events
/// </remarks>
public interface IBlockLifecycleListener : IEventSystemHandler
{
	void OnBlockDamaged(BlockCore blockCore);
	void OnBlockDestroyedByDamage(BlockCore blockCore);
}

public class BlockHealth : MonoBehaviour, IDamageable
{
	private BlockCore _core;
	private OwnerContext _ownerContext;

	public bool IsMine => _core.IsMine;
	public int OwnerId => _ownerContext.OwnerId;

	private float _maxHealth;
	private string _armorTypeId;

	private float _health;
	// Tracks destruction state to prevent destruction effects from being triggered multiple times.
	private bool _destroyed;
	public float HealthFraction => Mathf.Clamp01(_health / _maxHealth);
	public bool IsDamaged => _maxHealth - _health > Mathf.Epsilon;

	private Vector2 _explosionBoundsMin;
	private Vector2 _explosionBoundsMax;
	private int _explosionResolution;

	private void Awake()
	{
		_core = GetComponent<BlockCore>();
		_ownerContext = GetComponentInParent<OwnerContext>();
	}

	private void OnEnable()
	{
		_destroyed = false;
	}

	public void LoadSpec(BlockSpec spec)
	{
		_maxHealth = spec.Combat.MaxHealth;
		_armorTypeId = spec.Combat.ArmorTypeId;
		_explosionBoundsMin = spec.Construction.BoundsMin - new Vector2(0.5f, 0.5f);
		_explosionBoundsMax = spec.Construction.BoundsMax - new Vector2(0.5f, 0.5f);
		_explosionResolution = Mathf.Max(
			                       spec.Construction.BoundsMax.x - spec.Construction.BoundsMin.x,
			                       spec.Construction.BoundsMax.y - spec.Construction.BoundsMin.y
		                       )
		                       * 5;
	}

	private void Start()
	{
		_health = _maxHealth;
	}

	public (Vector2 Min, Vector2 Max) GetExplosionDamageBounds()
	{
		return (_explosionBoundsMin, _explosionBoundsMax);
	}

	public int GetExplosionGridResolution()
	{
		return _explosionResolution;
	}

	public Predicate<Vector2> GetPointInBoundPredicate()
	{
		return null;
	}

	public void TakeDamage(string damageType, ref float damage, float armorPierce, out bool damageExhausted)
	{
		float damageModifier = ArmorTypeDatabase.Instance.GetDamageModifier(damageType, armorPierce, _armorTypeId);

		// Damage modifier could be 0 if the armor type is completely immune to incoming damage
		if (Mathf.Approximately(damageModifier, 0f))
		{
			damage = 0f;
			damageExhausted = true;
			return;
		}

		float effectiveDamage = damage * damageModifier;
		float effectiveHealth = _health / damageModifier;

		if (effectiveDamage < _health)
		{
			_health -= effectiveDamage;
			damage = 0f;
			damageExhausted = true;

			ExecuteEvents.ExecuteHierarchy<IBlockLifecycleListener>(
				gameObject, null, (listener, _) => listener.OnBlockDamaged(_core)
			);
		}
		else
		{
			_health = 0f;
			damage -= effectiveHealth;
			damageExhausted = false;

			ExecuteEvents.ExecuteHierarchy<IBlockLifecycleListener>(
				gameObject, null, (listener, _) => listener.OnBlockDamaged(_core)
			);

			if (!_destroyed)
			{
				_destroyed = true;

				foreach (IBlockDestructionEffect effect in GetComponents<IBlockDestructionEffect>())
				{
					effect.OnDestroyedByDamage();
				}

				ExecuteEvents.ExecuteHierarchy<IBlockLifecycleListener>(
					gameObject, null, (listener, _) => listener.OnBlockDestroyedByDamage(_core)
				);
				// Note that, for multiplayer synchronization reasons, disabling of game object will be executed by VehicleCore
			}
		}
	}

	public void RequestBeamDamage(
		string damageType, float damage, float armorPierce, int ownerId,
		int? referenceFrameId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		ExecuteEvents.ExecuteHierarchy<IBlockRpcRelay>(
			gameObject, null,
			(relay, _) => relay.InvokeBlockRpc(
				_core.RootPosition, typeof(BlockHealth), nameof(TakeBeamDamageRpc), relay.photonView.Owner,
				damageType, damage, armorPierce, ownerId, referenceFrameId, beamStart, beamEnd
			)
		);
	}

	public void TakeBeamDamageRpc(
		string damageType, float damage, float armorPierce, int ownerId,
		int? referenceFrameId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		if (!IsMine) return;
		BeamWeaponUtils.HandleBeamDamage(
			damageType, damage, armorPierce, ownerId,
			referenceFrameId, beamStart, beamEnd
		);
	}
}
}