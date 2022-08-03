using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons.Launcher
{
[CreateSchemaFile("BurstBeamLauncherSpecSchema")]
public class BurstBeamLauncherSpec : AbstractWeaponLauncherSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ReloadTime;

	/// <summary>
	/// If true, uses precise duration behaviour. <code>BeamDurationTicks</code> is used and <code>BeamDurationSeconds</code> is ignored.
	/// If false, uses time-based duration behaviour. <code>BeamDurationSeconds</code> is used and <code>BeamDurationTicks</code> is ignored.
	/// </summary>
	public bool PreciseDuration;
	[ValidateRangeInt(0, int.MaxValue)]
	public int DurationTicks;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float DurationSeconds;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxRange;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float BeamWidth;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateColor(true)]
	public string BeamColor;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] HitParticles;

	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ScreenShakeSpec ScreenShake;
}

public class BurstBeamLauncher : AbstractWeaponLauncher
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

	private Dictionary<string, float> _reloadResourceUse;

	private float _reloadProgress;

	private ScreenShakeSpec _screenShake;

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

	public void LoadSpec(BurstBeamLauncherSpec spec, in BlockContext context)
	{
		base.LoadSpec(spec, context);

		_preciseDuration = spec.PreciseDuration;
		_durationSeconds = spec.DurationTicks;
		_durationSeconds = spec.DurationSeconds;

		_reloadTime = spec.ReloadTime;

		_totalDamage = spec.Damage;
		_damageType = spec.DamageType;
		_armorPierce = spec.ArmorPierce;
		_explosionRadius = spec.ExplosionRadius;
		MaxRange = spec.MaxRange;

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
		_screenShake = spec.ScreenShake;

		_raycastHits = new();
	}

	private void Start()
	{
		_reloadProgress = _reloadTime;
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		return _reloadProgress >= _reloadTime ? null : _reloadResourceUse;
	}

	public override void GetMaxFirepower(IList<FirepowerEntry> entries)
	{
		entries.Add(
			new()
			{
				DamageType = _damageType,
				DamagePerSecond = _totalDamage / _reloadTime,
				ArmorPierce = _damageType == DamageType.Explosive ? 1f : _armorPierce
			}
		);
	}

	public override string GetLauncherTooltip()
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
			.AppendLine($"    Max range {PhysicsUnitUtils.FormatDistance(MaxRange)}");

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(_reloadResourceUse));
		builder.AppendLine(
			_reloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"    Reload time {_reloadTime}s"
		);

		return builder.ToString();
	}

	public override Vector2? GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	)
	{
		return targetPosition;
	}

	public override void LauncherFixedUpdate(bool isMine, bool firing)
	{
		if (isMine)
		{
			if (firing && _reloadProgress >= _reloadTime)
			{
				_reloadProgress -= _reloadTime;

				StartBeamFiring();
				GetComponentInParent<IWeaponLauncherRpcRelay>().InvokeWeaponLauncherRpc(
					nameof(StartBeamFiring), RpcTarget.Others
				);
			}

			if (_reloadProgress < _reloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * ResourceSatisfaction;
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
		float correctionAngle = GetCorrectionAngle() * Mathf.Deg2Rad;
		Vector3 localEnd = new Vector3(MaxRange * Mathf.Sin(correctionAngle), MaxRange * Mathf.Cos(correctionAngle));
		Vector3 worldEnd = transform.TransformPoint(localEnd);

		Vector3? normal = null;
		IDamageable hitTarget = null;
		int count = Physics2D.Raycast(
			transform.position, transform.up, LayerConstants.WeaponHitFilter, _raycastHits, MaxRange
		);

		if (count > 0)
		{
			_raycastHits.Sort(0, count, RaycastHitComparer.Default);

			for (int i = 0; i < count; i++)
			{
				RaycastHit2D hit = _raycastHits[i];
				IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

				// Raycast needs to hit triggers for PD usage, but celestial bodies have big trigger colliders for gravity.
				// Create special case to ignore celestial body trigger colliders.
				if (hit.collider.isTrigger && hit.collider.gameObject.layer == LayerConstants.CELESTIAL_BODY_LAYER)
				{
					continue;
				}

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
					{
						if (hitTarget is IDirectDamageable directTarget)
						{
							directTarget.RequestDirectDamage(_damageType, damageThisTick, _armorPierce);
						}
						else
						{
							hitTarget.RequestBeamDamage(
								_damageType, damageThisTick, _armorPierce,
								_ownerContext.OwnerId,
								transform.position, transform.TransformPoint(new Vector3(0f, MaxRange))
							);
						}

						break;
					}
					case DamageType.Explosive:
					{
						Vector3 explosionCenter = worldEnd;
						var referenceFrame = hitTarget.transform.GetComponentInParent<ReferenceFrameProvider>();
						int? referenceFrameId = null;

						if (referenceFrame != null)
						{
							explosionCenter = referenceFrame.transform.InverseTransformPoint(worldEnd);
							referenceFrameId = referenceFrame.photonView.ViewID;
						}

						ExplosionManager.Instance.CreateExplosionAt(
							referenceFrameId, explosionCenter,
							_explosionRadius, damageThisTick, _ownerContext.OwnerId
						);
						break;
					}
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

		ExecuteWeaponSideEffects();
	}
}
}