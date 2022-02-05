using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Colors;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class BurstBeamWeaponEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
{
	private Camera _camera;
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

	private ScreenShakeSpec _screenShake;
	private AudioSource _audioSource;
	private AudioClip _fireSound;
	private float _fireSoundVolume;

	// State
	private int _beamTicksRemaining;
	private float _beamSecondsRemaining;

	private List<RaycastHit2D> _raycastHits;

	private BeamWeaponVisual _visual;

	private void Awake()
	{
		_camera = Camera.main;
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

		if (spec.FireSound != null)
		{
			_audioSource = SoundDatabase.Instance.CreateBlockAudioSource(gameObject);
			_fireSound = SoundDatabase.Instance.GetAudioClip(spec.FireSound.SoundId);
			_fireSoundVolume = spec.FireSound.Volume;
		}

		_reloadResourceUse = spec.MaxResourceUse;
		_screenShake = spec.ScreenShake;

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

	public float GetMaxRange()
	{
		return _maxRange;
	}

	public void GetMaxFirepower(IList<FirepowerEntry> entries)
	{
		entries.Add(
			new FirepowerEntry
			{
				DamageType = _damageType,
				DamagePerSecond = _totalDamage / _reloadTime,
				ArmorPierce = _damageType == DamageType.Explosive ? 1f : _armorPierce
			}
		);
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
					? $"{PhysicsUnitUtils.FormatLength(_explosionRadius)} radius"
					: $"<color=\"lightblue\">{_armorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Max range {PhysicsUnitUtils.FormatDistance(_maxRange)}");

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(_reloadResourceUse));
		builder.AppendLine(
			_reloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"    Reload time {_reloadTime}s"
		);

		return builder.ToString();
	}

	public void SetAimPoint(Vector2? aimPoint)
	{}

	public void SetTargetPhotonId(int? targetId)
	{}

	public Vector2 GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	)
	{
		return targetPosition;
	}

	public void EmitterFixedUpdate(bool isMine, bool firing)
	{
		if (isMine)
		{
			if (firing && _reloadProgress >= _reloadTime)
			{
				_reloadProgress -= _reloadTime;

				StartBeamFiring();
				GetComponentInParent<IWeaponEffectRpcRelay>().InvokeWeaponEffectRpc(
					nameof(StartBeamFiring), RpcTarget.Others
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

		if (isMine && damageThisTick > Mathf.Epsilon)
		{
			if (hitTarget != null)
			{
				switch (_damageType)
				{
					case DamageType.Kinetic:
					case DamageType.Energy:
						if (hitTarget is IDirectDamageable directTarget)
						{
							directTarget.RequestDirectDamage(_damageType, damageThisTick, _armorPierce);
						}
						else
						{
							hitTarget.RequestBeamDamage(
								_damageType, damageThisTick, _armorPierce,
								_ownerContext.OwnerId,
								transform.position, transform.TransformPoint(new Vector3(0f, _maxRange))
							);
						}

						break;
					case DamageType.Explosive:
						ExplosionManager.Instance.CreateExplosionAt(
							worldEnd, _explosionRadius, damageThisTick, _ownerContext.OwnerId,
							hitTarget.transform.GetComponentInParent<ReferenceFrameProvider>()?.GetVelocity()
						);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			if (_screenShake != null)
			{
				_camera.GetComponentInParent<CameraScreenShake>()?.AddInstance(
					_screenShake.Strength, _screenShake.Duration, _screenShake.DecayCurve
				);
			}
		}
	}

	public void StartBeamFiring()
	{
		if (_preciseDuration) _beamTicksRemaining = _durationTicks;
		else _beamSecondsRemaining = _durationSeconds;

		if (_fireSound != null)
		{
			_audioSource.PlayOneShot(_fireSound, _fireSoundVolume);
		}
	}
}
}