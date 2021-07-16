﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class TurretedBeamWeapon : TurretedWeapon, ITooltipProvider
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

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder.AppendLine("Turreted Beam Weapon")
			.AppendLine("  Beam")
			.AppendLine(
				Continuous
					? $"    Continuous {Damage:F0} {DamageTypeUtils.GetColoredText(DamageType)} damage/s, <color=\"lightblue\">{ArmorPierce:0.#} AP</color>"
					: $"    {Damage:F0} {DamageTypeUtils.GetColoredText(DamageType)} damage, <color=\"lightblue\">{ArmorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Max range {MaxRange * PhysicsConstants.METERS_PER_UNIT_LENGTH}m");

		string resourceCost = string.Join(
			" ", ReloadResourceConsumptionRate.Select(entry => $"{entry.RichTextColoredEntry()}/s")
		);
		if (Continuous)
		{
			if (ReloadResourceConsumptionRate.Length > 0)
			{
				builder.AppendLine($"    Firing cost {resourceCost}");
			}
		}
		else
		{
			builder.AppendLine(
				ReloadResourceConsumptionRate.Length > 0
					? $"    Beam duration {BeamDurationTicks * Time.fixedDeltaTime:0.0#}s, recharge time {ReloadTime}s, recharge cost {resourceCost}"
					: $"    Beam duration {BeamDurationTicks * Time.fixedDeltaTime:0.0#}, recharge time {ReloadTime}s"
			);
		}

		builder.AppendLine("  Turret")
			.AppendLine($"    Rotation speed {RotateSpeed}°/s");

		switch (SpreadProfile)
		{
			case WeaponSpreadProfile.None:
				break;
			case WeaponSpreadProfile.Gaussian:
				builder.AppendLine($"    Gaussian spread ±{SpreadAngle}°");
				break;
			case WeaponSpreadProfile.Uniform:
				builder.AppendLine($"    Uniform spread ±{SpreadAngle}°");
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		float maxDps = Continuous ? Damage : Damage / ReloadTime;
		float ap = DamageType == DamageType.Explosive ? 1f : ArmorPierce;
		float minArmorModifier = Mathf.Min(ap / 10f, 1f);
		builder.AppendLine($"  Theoretical maximum DPS vs 1 armor {maxDps:F1}");
		builder.AppendLine($"  Theoretical maximum DPS vs 10 armor {maxDps * minArmorModifier:F1}");

		return builder.ToString();
	}
}
}