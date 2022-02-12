using System;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation.Enums;
using UnityEngine;
using UnityEngine.Events;

namespace Syy1125.OberthEffect.WeaponEffect
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))] // Used by PD guns to plot intercept
public class PointDefenseTarget : MonoBehaviourPun, IDirectDamageable, IPunObservable, IGuidedWeaponTarget
{
	public bool IsMine { get; private set; }
	public int OwnerId { get; private set; }

	public UnityEvent OnDestroyedByDamage = new UnityEvent();

	private Rigidbody2D _body;

	private float _maxHealth;
	private float _health;
	private float _armor;
	private Vector2 _boundHalfSize;

	private bool _destroyed;

	public float HealthFraction => Mathf.Clamp01(_health / _maxHealth);

	private void Awake()
	{
		IsMine = photonView.IsMine;
		OwnerId = photonView.OwnerActorNr;

		_body = GetComponent<Rigidbody2D>();
	}

	public void Init(float maxHealth, float armorValue, Vector2 colliderSize)
	{
		_maxHealth = maxHealth;
		_armor = armorValue;
		_boundHalfSize = colliderSize / 2;
	}

	private void Start()
	{
		_health = _maxHealth;
	}

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(-_boundHalfSize, _boundHalfSize);
	}

	public int GetExplosionGridResolution()
	{
		return 3;
	}

	public Predicate<Vector2> GetPointInBoundPredicate()
	{
		return null;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_health);
		}
		else
		{
			_health = (float) stream.ReceiveNext();
		}
	}

	public Vector2 GetEffectivePosition()
	{
		return _body.worldCenterOfMass;
	}

	public Vector2 GetEffectiveVelocity()
	{
		return _body.velocity;
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
		else if (_health > 0f)
		{
			_health = 0f;
			damage -= effectiveHealth;
			damageExhausted = false;

			if (!_destroyed)
			{
				_destroyed = true;
				OnDestroyedByDamage.Invoke();
				// Whatever's listening to OnDestroyedByDamage is responsible for removing the projectile.
			}
		}
		else
		{
			damageExhausted = false;
		}
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		photonView.RPC(
			nameof(TakeBeamDamageRpc), RpcTarget.All,
			damageType, damage, armorPierce, ownerId, beamStart, beamEnd
		);
	}

	[PunRPC]
	private void TakeBeamDamageRpc(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		if (!IsMine) return;
		BeamWeaponUtils.HandleBeamDamage(damageType, damage, armorPierce, ownerId, beamStart, beamEnd);
	}

	public void RequestDirectDamage(
		DamageType damageType, float damage, float armorPierce
	)
	{
		photonView.RPC(nameof(TakeDirectDamageRpc), photonView.Owner, damageType, damage, armorPierce);

		// Predictive disabling if damage is expected to exceed health
		float damageModifier = Mathf.Min(armorPierce / _armor, 1f);
		float effectiveDamage = damage * damageModifier;
		if (_health > effectiveDamage)
		{
			_health -= effectiveDamage;
		}
		else
		{
			if (!_destroyed)
			{
				_destroyed = true;
				gameObject.SetActive(false);
			}
		}
	}

	[PunRPC]
	private void TakeDirectDamageRpc(DamageType damageType, float damage, float armorPierce)
	{
		if (!IsMine) return;
		TakeDamage(damageType, ref damage, armorPierce, out bool _);
	}
}

public struct PointDefenseTargetData
{
	public PointDefenseTarget Target;
	public float PriorityScore;
}
}