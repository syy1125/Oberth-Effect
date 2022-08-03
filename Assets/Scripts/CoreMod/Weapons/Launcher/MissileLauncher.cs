using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons.Launcher
{
public struct MissileLaunchTubeSpec
{
	public Vector2 Position;
	[ValidateRangeFloat(-360f, 360f)]
	public float Rotation;
	public Vector2 LaunchVelocity;
}

[CreateSchemaFile("MissileLauncherSpecSchema")]
public class MissileLauncherSpec : AbstractWeaponLauncherSpec
{
	public Vector2 ColliderSize;

	[ValidateNonNull]
	public MissileLaunchTubeSpec[] LaunchTubes;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float LaunchInterval;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxLifetime;

	public PointDefenseTargetSpec PointDefenseTarget;
	public float HealthDamageScaling = 0.75f;

	[ValidateNonNull]
	public IGuidanceSystemSpec GuidanceSystem;

	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ReloadTime;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] MissileRenderers;

	public ScreenShakeSpec ScreenShake;
}


public class MissileLauncher : AbstractWeaponLauncher
{
	private struct LaunchTube
	{
		public Transform FiringPort;
		public Vector2 LaunchVelocity;
		public float ReloadProgress;
	}

	private Camera _camera;
	private Rigidbody2D _body;
	private ColorContext _colorContext;

	private NetworkedProjectileConfig _missileConfig;
	private IGuidanceSystemSpec _guidanceSystem;
	private LaunchTube[] _launchTubes;
	private float _launchInterval;
	private float _reloadTime;

	private ScreenShakeSpec _screenShake;

	// State
	private int _activeLauncherIndex;
	private float _launchCooldown;

	private void Awake()
	{
		_camera = Camera.main;
		_body = GetComponentInParent<Rigidbody2D>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(MissileLauncherSpec spec, in BlockContext context)
	{
		base.LoadSpec(spec, context);

		_missileConfig = new()
		{
			ColliderSize = spec.ColliderSize,
			Damage = spec.Damage,
			DamageType = spec.DamageType,
			ArmorPierce = spec.ArmorPierce,
			ExplosionRadius = spec.ExplosionRadius,
			Lifetime = spec.MaxLifetime,
			Renderers = spec.MissileRenderers,
			ColorScheme = _colorContext.ColorScheme,
		};

		_guidanceSystem = spec.GuidanceSystem;
		switch (_guidanceSystem)
		{
			case PredictiveGuidanceSystemSpec:
				_missileConfig.ProjectileComponents.Add("PredictiveGuidance", _guidanceSystem);
				break;
			default:
				Debug.LogError($"Unexpected guidance system type `{_guidanceSystem.GetType()}`");
				break;
		}

		if (spec.PointDefenseTarget != null)
		{
			_missileConfig.PointDefenseTarget = spec.PointDefenseTarget;
			_missileConfig.HealthDamageScaling = spec.HealthDamageScaling;
		}

		_launchTubes = new LaunchTube[spec.LaunchTubes.Length];
		for (int i = 0; i < spec.LaunchTubes.Length; i++)
		{
			var firingPort = new GameObject($"Missile Tube {i + 1}").transform;
			firingPort.SetParent(transform);
			firingPort.localPosition = spec.LaunchTubes[i].Position;
			firingPort.localRotation = Quaternion.AngleAxis(spec.LaunchTubes[i].Rotation, Vector3.forward);
			firingPort.localScale = Vector3.one;

			RendererHelper.AttachRenderers(firingPort, _missileConfig.Renderers);

			_launchTubes[i] = new LaunchTube
			{
				FiringPort = firingPort,
				LaunchVelocity = spec.LaunchTubes[i].LaunchVelocity,
				ReloadProgress = spec.ReloadTime
			};
		}

		_launchInterval = spec.LaunchInterval;

		_reloadTime = spec.ReloadTime;
		ReloadResourceUse = spec.MaxResourceUse;

		_screenShake = spec.ScreenShake;

		// r = v * t + 1/2 * a * t^2
		MaxRange = _guidanceSystem.GetMaxRange(
			spec.LaunchTubes.Max(tube => tube.LaunchVelocity.magnitude), _missileConfig.Lifetime
		);
	}

	public override IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		int reloadCount = _launchTubes.Count(tube => tube.ReloadProgress < _reloadTime);
		float multiplier = (float) reloadCount / _launchTubes.Length;
		return ReloadResourceUse.ToDictionary(entry => entry.Key, entry => entry.Value * multiplier);
	}

	public override Vector2? GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	)
	{
		return _guidanceSystem.GetInterceptPoint(ownPosition, ownVelocity, targetPosition, targetVelocity);
	}

	public override void LauncherFixedUpdate(bool isMine, bool firing)
	{
		if (isMine)
		{
			if (firing)
			{
				while (_launchCooldown <= 0f && FindNextReadyLauncher())
				{
					FireCurrentTube();
					_launchCooldown = _launchInterval;
				}
			}

			_launchCooldown -= Time.fixedDeltaTime;
			ProgressReload();
		}
	}

	private bool FindNextReadyLauncher()
	{
		for (int i = 0; i < _launchTubes.Length; i++)
		{
			int index = (_activeLauncherIndex + i) % _launchTubes.Length;
			if (_launchTubes[index].ReloadProgress >= _reloadTime)
			{
				_activeLauncherIndex = index;
				return true;
			}
		}

		return false;
	}

	private void FireCurrentTube()
	{
		if (AimPoint == null) return;

		var firingPort = _launchTubes[_activeLauncherIndex].FiringPort;
		Vector3 localLaunchVelocity = _launchTubes[_activeLauncherIndex].LaunchVelocity;

		Vector2 initialVelocity = _body.GetPointVelocity(firingPort.position)
		                          + (Vector2) transform.TransformVector(localLaunchVelocity);

		GameObject missile = NetworkedProjectileManager.Instance.CreateProjectile(
			firingPort.position, firingPort.rotation, initialVelocity,
			_missileConfig
		);

		foreach (var component in missile.GetComponents<IRemoteControlledProjectileComponent>())
		{
			component.Launcher = this;
		}

		firingPort.gameObject.SetActive(false);
		GetComponentInParent<IWeaponLauncherRpcRelay>().InvokeWeaponLauncherRpc(
			nameof(SetLaunchTubeVisualFull), RpcTarget.Others, _activeLauncherIndex, false
		);
		_launchTubes[_activeLauncherIndex].ReloadProgress -= _reloadTime;

		if (_screenShake != null)
		{
			_camera.GetComponentInParent<CameraScreenShake>()?.AddInstance(
				_screenShake.Strength, _screenShake.Duration, _screenShake.DecayCurve
			);
		}

		ExecuteWeaponSideEffects();
	}

	private void ProgressReload()
	{
		for (int i = 0; i < _launchTubes.Length; i++)
		{
			if (_launchTubes[i].ReloadProgress < _reloadTime)
			{
				_launchTubes[i].ReloadProgress += Time.fixedDeltaTime * ResourceSatisfaction;

				if (_launchTubes[i].ReloadProgress >= _reloadTime)
				{
					_launchTubes[i].FiringPort.gameObject.SetActive(true);
					GetComponentInParent<IWeaponLauncherRpcRelay>().InvokeWeaponLauncherRpc(
						nameof(SetLaunchTubeVisualFull), RpcTarget.Others, i, true
					);
				}
			}
		}
	}

	public void SetLaunchTubeVisualFull(int tubeIndex, bool active)
	{
		_launchTubes[tubeIndex].FiringPort.gameObject.SetActive(active);
	}

	public override void GetMaxFirepower(IList<FirepowerEntry> entries)
	{
		entries.Add(
			new()
			{
				DamagePerSecond = _missileConfig.Damage * _launchTubes.Length / _reloadTime,
				DamageType = _missileConfig.DamageType,
				ArmorPierce = _missileConfig.DamageType == DamageType.Explosive ? 1f : _missileConfig.ArmorPierce
			}
		);
	}

	public override string GetLauncherTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder.AppendLine("  Missile")
			.AppendLine(
				_missileConfig.DamageType == DamageType.Explosive
					? $"    {_missileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_missileConfig.DamageType)} damage, {PhysicsUnitUtils.FormatLength(_missileConfig.ExplosionRadius)} radius"
					: $"    {_missileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_missileConfig.DamageType)} damage, <color=\"lightblue\">{_missileConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine(_guidanceSystem.GetGuidanceSystemTooltip())
			.AppendLine($"    Max range {PhysicsUnitUtils.FormatDistance(MaxRange)}");

		if (_missileConfig.PointDefenseTarget != null)
		{
			builder.AppendLine(
				$"    Missile has <color=\"red\">{_missileConfig.PointDefenseTarget.MaxHealth} health</color>, <color=\"lightblue\">{_missileConfig.PointDefenseTarget.ArmorValue} armor</color>"
			);
			if (!Mathf.Approximately(_missileConfig.HealthDamageScaling, 0f))
			{
				builder.AppendLine(
					$"    Damage reduced by up to {_missileConfig.HealthDamageScaling:00.#%}, scaling with fraction of health lost"
				);
			}
		}

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(ReloadResourceUse));
		builder.AppendLine(
			ReloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"    Reload time {_reloadTime}"
		);

		return builder.ToString();
	}
}
}