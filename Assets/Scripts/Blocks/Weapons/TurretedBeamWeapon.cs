using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class TurretedBeamWeapon : TurretedWeapon
{
	[Header("Beam Weapon Config")]
	public GameObject EffectRenderer;

	public DamageType DamageType;
	public bool Continuous;
	// If continuous, damage is interpreted as damage per second.
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce;
	public int BeamDurationTicks;
	public float MaxRange;
	public float RangeAttenuation;

	private float _tickDamage;
	private int _beamDurationRemaining;

	protected override void Start()
	{
		base.Start();

		ExecuteEvents.Execute<IBeamWeaponVisualEffect>(
			EffectRenderer, null,
			(handler, _) => handler.SetColorScheme(ColorContext.ColorScheme)
		);
		_tickDamage = Continuous ? Damage * Time.fixedDeltaTime : Damage / BeamDurationTicks;
	}

	public override IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest()
	{
		return Continuous
			? Firing ? ResourceConsumption : null
			: base.GetResourceConsumptionRateRequest();
	}

	protected override void FireFixedUpdate()
	{
		if (Continuous)
		{
			if (Firing)
			{
				FireBeamTick(_tickDamage * ResourceSatisfactionLevel);
			}
			else
			{
				EffectRenderer.gameObject.SetActive(false);
			}
		}
		else
		{
			base.FireFixedUpdate();

			if (_beamDurationRemaining > 0)
			{
				FireBeamTick();
				_beamDurationRemaining--;
			}
			else
			{
				EffectRenderer.gameObject.SetActive(false);
			}
		}
	}

	protected override void Fire()
	{
		_beamDurationRemaining = BeamDurationTicks;
	}

	private void FireBeamTick()
	{
		FireBeamTick(_tickDamage);
	}

	private void FireBeamTick(float damage)
	{
		Vector3 start = FiringPort.position;

		EffectRenderer.gameObject.SetActive(true);
		ExecuteEvents.Execute<IBeamWeaponVisualEffect>(
			EffectRenderer, null,
			(handler, _) => handler.SetBeamPoints(
				start, start + FiringPort.up * MaxRange, false
			)
		);
	}

	public override Dictionary<DamageType, float> GetDamageRatePotential()
	{
		return new Dictionary<DamageType, float>
		{
			{ DamageType, Continuous ? Damage : Damage / ReloadTime }
		};
	}
}
}