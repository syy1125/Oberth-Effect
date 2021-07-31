using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
[RequireComponent(typeof(BlockCore))]
public class TurretedBeamWeapon : AbstractTurretedWeapon, ITooltipProvider
{
	[Header("Beam Weapon Config")]
	public GameObject EffectRenderer;
	public LayerMask HitLayerMask;

	public DamageType DamageType;
	public bool Continuous;
	// If continuous, damage is interpreted as damage per second.
	public float Damage;
	[Range(1, 10)]
	public float ArmorPierce;
	public int BeamDurationTicks;
	public float MaxRange;
	public float RangeAttenuation;

	private BlockCore _core;

	private float _tickDamage;
	private ContactFilter2D _raycastFilter;
	private List<RaycastHit2D> _raycastHits;
	private Comparer<RaycastHit2D> _hitComparer;

	private int _beamDurationRemaining;

	protected override void Awake()
	{
		base.Awake();

		_core = GetComponent<BlockCore>();
	}

	protected override void Start()
	{
		base.Start();

		ExecuteEvents.Execute<IBeamWeaponVisualEffect>(
			EffectRenderer, null,
			(handler, _) => handler.SetColorScheme(ColorContext.ColorScheme)
		);
		_tickDamage = Continuous ? Damage * Time.fixedDeltaTime : Damage / BeamDurationTicks;
		_raycastFilter = new ContactFilter2D
		{
			layerMask = HitLayerMask,
			useLayerMask = true
		};
		_raycastHits = new List<RaycastHit2D>();
		_hitComparer = Comparer<RaycastHit2D>.Create(CompareRaycastHitDistance);
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return Continuous
			? Firing ? ReloadResourceUse : null
			: base.GetResourceConsumptionRateRequest();
	}

	protected override void FireFixedUpdate()
	{
		if (Continuous)
		{
			if (Firing)
			{
				if (_beamDurationRemaining <= 0)
				{
					_beamDurationRemaining = 1;
					SendBeamDuration();
				}

				FireBeamTick(_tickDamage * ResourceSatisfactionLevel);
			}
			else
			{
				if (_beamDurationRemaining > 0)
				{
					_beamDurationRemaining = 0;
					SendBeamDuration();
				}

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

	private void SendBeamDuration()
	{
		ExecuteEvents.ExecuteHierarchy<IBlockRpcRelay>(
			gameObject, null,
			(relay, _) => relay.InvokeBlockRpc(
				_core.RootPosition, typeof(TurretedBeamWeapon), "SetVisualFiring", RpcTarget.Others,
				_beamDurationRemaining
			)
		);
	}

	protected override void Fire()
	{
		_beamDurationRemaining = BeamDurationTicks;
		SendBeamDuration();
	}

	private void FireBeamTick()
	{
		FireBeamTick(_tickDamage);
	}

	private void FireBeamTick(float damage)
	{
		ShowBeamVisual();
	}

	private static int CompareRaycastHitDistance(RaycastHit2D x, RaycastHit2D y)
	{
		return 0;
	}

	public void SetVisualFiring(int ticks)
	{
		_beamDurationRemaining = ticks;
	}

	protected override void VisualFixedUpdate()
	{
		if (Continuous)
		{
			if (_beamDurationRemaining > 0)
			{
				ShowBeamVisual();
			}
			else
			{
				EffectRenderer.SetActive(false);
			}
		}
		else
		{
			if (_beamDurationRemaining > 0)
			{
				ShowBeamVisual();
				_beamDurationRemaining--;
			}
			else
			{
				EffectRenderer.SetActive(false);
			}
		}
	}

	private void ShowBeamVisual()
	{
		Vector3 start = FiringPort.position;

		bool hitTarget = false;
		Vector3 end = start + FiringPort.up * MaxRange;
		int count = Physics2D.Raycast(start, FiringPort.up, _raycastFilter, _raycastHits, MaxRange);

		if (count > 0)
		{
			Debug.Log($"Visual raycast hit {count} targets");
			_raycastHits.Sort(0, count, _hitComparer);

			for (int i = 0; i < count; i++)
			{
				RaycastHit2D hit = _raycastHits[i];
				IDamageable target = ComponentUtils.GetBehaviourInParent<IDamageable>(hit.collider.transform);

				if (target != null && target.OwnerId != GetOwnerId())
				{
					hitTarget = true;
					end = hit.point;
					break;
				}
			}
		}

		EffectRenderer.gameObject.SetActive(true);
		ExecuteEvents.Execute<IBeamWeaponVisualEffect>(
			EffectRenderer, null,
			(handler, _) => handler.SetBeamPoints(start, end, hitTarget)
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
			", ", VehicleResourceDatabase.Instance.FormatResourceDict(ReloadResourceUse)
		);
		if (Continuous)
		{
			if (ReloadResourceUse.Count > 0)
			{
				builder.AppendLine($"    Firing cost {resourceCost}");
			}
		}
		else
		{
			builder.AppendLine(
				ReloadResourceUse.Count > 0
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