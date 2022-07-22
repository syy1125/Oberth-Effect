using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Foundation.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(BoxCollider2D))]
public class TargetDummy : MonoBehaviour, IDamageable, ITargetLockInfoProvider
{
	public struct DamageInstance
	{
		public DamageType Type;
		public float Amount;
		public float Time;
	}

	public float ArmorValue = 1f;
	public float TimeInterval = 1f;
	public Text Display;

	public bool IsMine => true;
	public int OwnerId => -1;

	private BoxCollider2D _collider;
	private LinkedList<DamageInstance> _damage;
	public float DamagePerSecond { get; private set; }

	private void Awake()
	{
		_collider = GetComponent<BoxCollider2D>();
		_damage = new LinkedList<DamageInstance>();
	}

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return Tuple.Create(_collider.offset - _collider.size / 2, _collider.offset + _collider.size / 2);
	}

	public int GetExplosionGridResolution()
	{
		return 5;
	}

	public Predicate<Vector2> GetPointInBoundPredicate()
	{
		return null;
	}

	public void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted)
	{
		float armorModifier = Mathf.Min(armorPierce / ArmorValue, 1f);

		float effectiveDamage = damage * armorModifier;
		_damage.AddLast(new DamageInstance { Type = damageType, Amount = effectiveDamage, Time = Time.time });

		damageExhausted = true;
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		TakeDamage(damageType, ref damage, armorPierce, out bool _);
	}

	public string GetName()
	{
		return $"Target Dummy ({ArmorValue:0.#} Armor, {TimeInterval:0.#}s)";
	}

	public Vector2 GetPosition()
	{
		return transform.position;
	}

	public Vector2 GetVelocity()
	{
		return Vector2.zero;
	}

	private void LateUpdate()
	{
		float minTime = Time.time - TimeInterval;

		while (_damage.Count > 0 && _damage.First.Value.Time < minTime)
		{
			_damage.RemoveFirst();
		}

		DamagePerSecond = _damage.Sum(instance => instance.Amount) / TimeInterval;

		Display.text = string.Join(
			"\n",
			$"{ArmorValue:0.#} Armor, interval={TimeInterval:0.#}s",
			$"DPS={DamagePerSecond:0.0#}"
		);
	}

	public IEnumerable<DamageInstance> GetDamageHistory()
	{
		return _damage;
	}

	public void ResetDamage()
	{
		_damage.Clear();
	}
}
}