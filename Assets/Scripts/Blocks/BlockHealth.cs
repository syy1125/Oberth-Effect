using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Utils;
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

	private ContactFilter2D _beamRaycastFilter;
	private List<RaycastHit2D> _beamRaycastHits;

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

		_beamRaycastFilter = new ContactFilter2D
		{
			layerMask = WeaponConstants.HIT_LAYER_MASK,
			useLayerMask = true
		};
		_beamRaycastHits = new List<RaycastHit2D>();
	}

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(_boundsMin, _boundsMax);
	}

	public void TakeDamage(
		DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted
	)
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

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		ExecuteEvents.ExecuteHierarchy<IBlockRpcRelay>(
			gameObject, null,
			(relay, _) => relay.InvokeBlockRpc(
				_core.RootPosition, typeof(BlockHealth), nameof(TakeBeamDamageRpc), RpcTarget.All,
				damageType, damage, armorPierce, ownerId, beamStart, beamEnd
			)
		);
	}

	public void TakeBeamDamageRpc(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 hitPosition, Vector2 beamEnd
	)
	{
		if (!IsMine) return;

		Vector2 beamDirection = beamEnd - hitPosition;
		int count = Physics2D.Raycast(
			hitPosition, beamDirection, _beamRaycastFilter, _beamRaycastHits, beamDirection.magnitude
		);

		if (count > 0)
		{
			_beamRaycastHits.Sort(0, count, RaycastHitComparer.Default);

			for (int i = 0; i < count; i++)
			{
				RaycastHit2D hit = _beamRaycastHits[i];
				IDamageable target = ComponentUtils.GetBehaviourInParent<IDamageable>(hit.collider.transform);

				// Only do damage calculations for blocks we own
				if (target == null || target.OwnerId == ownerId || !target.IsMine) continue;

				target.TakeDamage(damageType, ref damage, armorPierce, out bool damageExhausted);

				if (damageExhausted) break;
			}
		}
	}
}
}