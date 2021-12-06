using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class BurstBeamWeaponEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
{
	private OwnerContext _ownerContext;
	private ColorContext _colorContext;

	private bool _preciseDuration;
	private int _durationTicks;
	private float _durationSeconds;
	private float _reloadTime;

	private float _totalDamage;
	private DamageType _damageType;
	private float _armorPierce;
	private float _explosionRadius;
	private float _maxRange;

	private Dictionary<string, float> _reloadResourceUse;

	private float _reloadProgress;
	private float _resourceSatisfaction;

	private int _beamTicksRemaining;
	private float _beamSecondsRemaining;

	private List<RaycastHit2D> _raycastHits;

	private BeamWeaponVisual _visual;

	private void Awake()
	{
		_ownerContext = GetComponentInParent<OwnerContext>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(BurstBeamWeaponEffectSpec spec)
	{
		_preciseDuration = spec.PreciseDuration;
		_durationTicks = spec.DurationTicks;
		_durationSeconds = spec.DurationSeconds;

		_reloadTime = spec.ReloadTime;

		_totalDamage = spec.Damage;
		_damageType = spec.DamageType;
		_armorPierce = spec.ArmorPierce;
		_explosionRadius = spec.ExplosionRadius;
		_maxRange = spec.MaxRange;

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
			{ _damageType, _totalDamage / _reloadTime }
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
			.Append($"    {_totalDamage:F0} {DamageTypeUtils.GetColoredText(_damageType)} damage over ")
			.Append(_preciseDuration ? $"{_durationTicks} tick(s)" : $"{_durationSeconds} second(s)")
			.Append(", ")
			.AppendLine(
				_damageType == DamageType.Explosive
					? $"{_explosionRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m radius"
					: $"<color=\"lightblue\">{_armorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Max range {_maxRange * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m");

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(_reloadResourceUse));
		builder.AppendLine(
			_reloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"    Reload time {_reloadTime}s"
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

				StartBeamDuration();
				GetComponentInParent<IWeaponEffectRpcRelay>().InvokeWeaponEffectRpc(
					this, nameof(StartBeamDuration), RpcTarget.Others
				);
			}

			if (_reloadProgress < _reloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * _resourceSatisfaction;
			}
		}

		if (_preciseDuration)
		{
			if (Time.fixedDeltaTime < Mathf.Epsilon)
			{
				FireBeamTick(isMine, 0f);
			}

			if (_beamTicksRemaining-- > 0)
			{
				FireBeamTick(isMine, _totalDamage / _durationTicks);
			}
			else
			{
				_visual.UpdateState(false, Vector3.zero, null);
			}
		}
		else
		{
			float deltaTime = Time.fixedDeltaTime;

			if (_beamSecondsRemaining > deltaTime)
			{
				FireBeamTick(isMine, _totalDamage * deltaTime / _durationSeconds);
				_beamSecondsRemaining -= deltaTime;
			}
			else if (_beamSecondsRemaining > 0f)
			{
				FireBeamTick(isMine, _totalDamage * _beamSecondsRemaining / _durationSeconds);
				_beamSecondsRemaining = 0f;
			}
			else
			{
				_visual.UpdateState(false, Vector3.zero, null);
			}
		}
	}

	private void FireBeamTick(bool isMine, float damageThisTick)
	{
		Vector3 worldEnd = transform.TransformPoint(new Vector3(0f, _maxRange, 0f));
		Vector3? normal = null;
		IDamageable hitTarget = null;
		int count = Physics2D.Raycast(
			transform.position, transform.up, LayerConstants.WeaponHitFilter, _raycastHits, _maxRange
		);

		if (count > 0)
		{
			_raycastHits.Sort(0, count, RaycastHitComparer.Default);

			for (int i = 0; i < count; i++)
			{
				RaycastHit2D hit = _raycastHits[i];
				IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

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

		if (isMine && damageThisTick > Mathf.Epsilon && hitTarget != null)
		{
			switch (_damageType)
			{
				case DamageType.Kinetic:
				case DamageType.Energy:
					hitTarget.RequestBeamDamage(
						_damageType, damageThisTick, _armorPierce,
						_ownerContext.OwnerId,
						transform.position, transform.TransformPoint(new Vector3(0f, _maxRange))
					);
					break;
				case DamageType.Explosive:
					ExplosionManager.Instance.CreateExplosionAt(
						worldEnd, _explosionRadius, damageThisTick, _ownerContext.OwnerId
					);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public void StartBeamDuration()
	{
		if (_preciseDuration) _beamTicksRemaining = _durationTicks;
		else _beamSecondsRemaining = _durationSeconds;
	}
}
}