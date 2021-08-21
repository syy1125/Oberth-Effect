using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class BurstBeamWeaponEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
{
	private OwnerContext _ownerContext;
	private ColorContext _colorContext;

	private int _durationTicks;
	private float _reloadTime;
	private BeamTickConfig _beamConfig;

	private Dictionary<string, float> _reloadResourceUse;

	private float _reloadProgress;
	private float _resourceSatisfaction;

	private int _beamTicksRemaining;

	private ContactFilter2D _raycastFilter;
	private List<RaycastHit2D> _raycastHits;

	private BeamWeaponVisual _visual;

	private void Awake()
	{
		_ownerContext = GetComponentInParent<OwnerContext>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(BurstBeamWeaponEffectSpec spec)
	{
		_durationTicks = spec.PreciseDuration
			? spec.DurationTicks
			: Mathf.RoundToInt(spec.DurationSeconds / Time.fixedDeltaTime);

		_reloadTime = spec.ReloadTime;

		_beamConfig = new BeamTickConfig
		{
			DamagePerTick = spec.Damage / _durationTicks,
			DamageType = spec.DamageType,
			ArmorPierce = spec.ArmorPierce,
			ExplosionRadius = spec.ExplosionRadius,
			MaxRange = spec.MaxRange
		};

		float beamWidth = spec.BeamWidth;
		if (!_colorContext.ColorScheme.ResolveColor(spec.BeamColor, out Color beamColor))
		{
			Debug.LogError($"Failed to parse beam color {spec.BeamColor}");
		}

		var beamEffectObject = new GameObject("BeamEffect");
		var beamEffectTransform = beamEffectObject.transform;
		beamEffectTransform.SetParent(transform, false);
		beamEffectTransform.localPosition = Vector3.zero;
		beamEffectTransform.localRotation = Quaternion.identity;
		beamEffectTransform.localScale = Vector3.one;

		_visual = beamEffectObject.AddComponent<BeamWeaponVisual>();
		_visual.Init(beamWidth, beamColor, spec.HitParticles);

		_reloadResourceUse = spec.MaxResourceUse;

		_raycastFilter = new ContactFilter2D
		{
			layerMask = WeaponConstants.HIT_LAYER_MASK,
			useLayerMask = true
		};
		_raycastHits = new List<RaycastHit2D>();
	}

	private void Start()
	{
		_reloadProgress = _reloadTime;
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return _reloadProgress >= _reloadTime ? null : _reloadResourceUse;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		_resourceSatisfaction = level;
	}

	public IReadOnlyDictionary<DamageType, float> GetMaxFirepower()
	{
		return new Dictionary<DamageType, float>
		{
			{ _beamConfig.DamageType, _beamConfig.DamagePerTick * _durationTicks / _reloadTime }
		};
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		return _reloadResourceUse;
	}

	public string GetEmitterTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder
			.AppendLine("  Burst Beam")
			.AppendLine(
				_beamConfig.DamageType == DamageType.Explosive
					? $"    {_beamConfig.DamagePerTick:F0} {DamageTypeUtils.GetColoredText(_beamConfig.DamageType)} damage, {_beamConfig.ExplosionRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m radius"
					: $"    {_beamConfig.DamagePerTick:F0} {DamageTypeUtils.GetColoredText(_beamConfig.DamageType)} damage, <color=\"lightblue\">{_beamConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Max range {_beamConfig.MaxRange * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m");

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(_reloadResourceUse));
		builder.AppendLine(
			_reloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}"
				: $"    Reload time {_reloadTime}"
		);

		return builder.ToString();
	}

	public void EmitterFixedUpdate(bool firing, bool isMine)
	{
		if (isMine)
		{
			if (firing && _reloadProgress >= _reloadTime)
			{
				_reloadProgress -= _reloadTime;

				_beamTicksRemaining = _durationTicks;
				ExecuteEvents.ExecuteHierarchy<IWeaponEffectRpcRelay>(
					gameObject, null,
					(relay, _) => relay.InvokeWeaponEffectRpc(
						this, nameof(SetBeamTicksRemaining), RpcTarget.Others, _durationTicks
					)
				);
			}

			if (_reloadProgress < _reloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * _resourceSatisfaction;
			}
		}

		if (_beamTicksRemaining-- > 0)
		{
			Vector3 worldEnd = transform.TransformPoint(new Vector3(0f, _beamConfig.MaxRange, 0f));
			Vector3? normal = null;
			IDamageable hitTarget = null;
			int count = Physics2D.Raycast(
				transform.position, transform.up, _raycastFilter, _raycastHits, _beamConfig.MaxRange
			);

			if (count > 0)
			{
				_raycastHits.Sort(0, count, RaycastHitComparer.Default);

				for (int i = 0; i < count; i++)
				{
					RaycastHit2D hit = _raycastHits[i];
					IDamageable target = ComponentUtils.GetBehaviourInParent<IDamageable>(hit.collider.transform);

					if (target != null && target.OwnerId != _ownerContext.OwnerId)
					{
						hitTarget = target;
						worldEnd = hit.point;
						normal = hit.normal;
						break;
					}
				}
			}

			_visual.UpdateState(true, transform.InverseTransformPoint(worldEnd), normal);

			if (isMine && hitTarget != null)
			{
				switch (_beamConfig.DamageType)
				{
					case DamageType.Kinetic:
					case DamageType.Energy:
						hitTarget.RequestBeamDamage(
							_beamConfig.DamageType, _beamConfig.DamagePerTick, _beamConfig.ArmorPierce,
							_ownerContext.OwnerId,
							transform.position, transform.TransformPoint(new Vector3(0f, _beamConfig.MaxRange))
						);
						break;
					case DamageType.Explosive:
						ExplosionManager.Instance.CreateExplosionAt(
							worldEnd, _beamConfig.ExplosionRadius, _beamConfig.DamagePerTick
						);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
		else
		{
			_visual.UpdateState(false, Vector3.zero, null);
		}
	}

	public void SetBeamTicksRemaining(int ticks)
	{
		_beamTicksRemaining = ticks;
	}
}
}