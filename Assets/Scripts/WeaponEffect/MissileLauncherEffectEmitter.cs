using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Colors;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public class MissileLauncherEffectEmitter : MonoBehaviour, IWeaponEffectEmitter
{
	private struct LaunchTube
	{
		public Transform FiringPort;
		public Vector2 LaunchVelocity;
		public float ReloadProgress;
	}

	private Rigidbody2D _body;
	private ColorContext _colorContext;

	private MissileConfig _missileConfig;
	private LaunchTube[] _launchTubes;
	private float _launchInterval;
	private float _reloadTime;
	private Dictionary<string, float> _reloadResourceUse;

	private float _maxRange;

	// State
	private Vector2? _aimPoint;
	private float _resourceSatisfaction;
	private int _activeLauncherIndex;
	private float _launchCooldown;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_colorContext = GetComponentInParent<ColorContext>();
	}

	public void LoadSpec(MissileLauncherEffectSpec spec)
	{
		_missileConfig = new MissileConfig
		{
			ColliderSize = spec.ColliderSize,
			Damage = spec.Damage,
			DamageType = spec.DamageType,
			ArmorPierce = spec.ArmorPierce,
			ExplosionRadius = spec.ExplosionRadius,
			Lifetime = spec.MaxLifetime,
			MaxAcceleration = spec.MaxAcceleration,
			MaxAngularAcceleration = spec.MaxAngularAcceleration,
			GuidanceAlgorithm = spec.GuidanceAlgorithm,
			GuidanceActivationDelay = spec.GuidanceActivationDelay,
			Renderers = spec.Renderers
		};

		if (spec.PointDefenseTarget != null)
		{
			_missileConfig.IsPointDefenseTarget = true;
			_missileConfig.MaxHealth = spec.PointDefenseTarget.MaxHealth;
			_missileConfig.ArmorValue = spec.PointDefenseTarget.ArmorValue;
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
		_reloadResourceUse = spec.MaxResourceUse;

		// r = v * t + 1/2 * a * t^2
		_maxRange = spec.LaunchTubes.Max(tube => tube.LaunchVelocity.magnitude) * _missileConfig.Lifetime
		            + 0.5f * _missileConfig.MaxAcceleration * _missileConfig.Lifetime * _missileConfig.Lifetime;
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		int reloadCount = _launchTubes.Count(tube => tube.ReloadProgress < _reloadTime);
		float multiplier = (float) reloadCount / _launchTubes.Length;
		return _reloadResourceUse.ToDictionary(entry => entry.Key, entry => entry.Value * multiplier);
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		_resourceSatisfaction = level;
	}

	public void SetAimPoint(Vector2? aimPoint)
	{
		_aimPoint = aimPoint;
	}

	public void EmitterFixedUpdate(bool isMine, bool firing)
	{
		if (isMine)
		{
			if (firing)
			{
				while (_launchCooldown <= 0f && FindNextReadyLauncher())
				{
					FireCurrentLauncher();
					_launchCooldown = _launchInterval;
				}
			}

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

	private void FireCurrentLauncher()
	{
		var firingPort = _launchTubes[_activeLauncherIndex].FiringPort;
		Vector3 localLaunchVelocity = _launchTubes[_activeLauncherIndex].LaunchVelocity;

		GameObject missile = PhotonNetwork.Instantiate(
			"Missile", firingPort.position, firingPort.rotation,
			data: new object[]
			{
				CompressionUtils.Compress(JsonUtility.ToJson(_missileConfig)),
				JsonUtility.ToJson(_colorContext.ColorScheme)
			}
		);

		var missileBody = missile.GetComponent<Rigidbody2D>();
		missileBody.velocity =
			_body.GetPointVelocity(firingPort.position)
			+ (Vector2) transform.TransformVector(localLaunchVelocity);

		firingPort.gameObject.SetActive(false);
		_launchTubes[_activeLauncherIndex].ReloadProgress -= _reloadTime;
	}

	private void ProgressReload()
	{
		for (int i = 0; i < _launchTubes.Length; i++)
		{
			if (_launchTubes[i].ReloadProgress < _reloadTime)
			{
				_launchTubes[i].ReloadProgress += Time.fixedDeltaTime * _resourceSatisfaction;

				if (_launchTubes[i].ReloadProgress >= _reloadTime)
				{
					_launchTubes[i].FiringPort.gameObject.SetActive(true);
				}
			}
		}
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
				DamagePerSecond = _missileConfig.Damage * _launchTubes.Length / _reloadTime,
				DamageType = _missileConfig.DamageType,
				ArmorPierce = _missileConfig.DamageType == DamageType.Explosive ? 1f : _missileConfig.ArmorPierce
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

		builder.AppendLine("  Missile")
			.AppendLine(
				_missileConfig.DamageType == DamageType.Explosive
					? $"    {_missileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_missileConfig.DamageType)} damage, {_missileConfig.ExplosionRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH}m radius"
					: $"    {_missileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(_missileConfig.DamageType)} damage, <color=\"lightblue\">{_missileConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Max range {_maxRange * PhysicsConstants.METERS_PER_UNIT_LENGTH}m");

		if (_missileConfig.IsPointDefenseTarget)
		{
			builder.AppendLine(
				$"    Missile has <color=\"red\">{_missileConfig.MaxHealth} health</color>, <color=\"lightblue\">{_missileConfig.ArmorValue} armor</color>"
			);
		}

		string reloadCost = string.Join(" ", VehicleResourceDatabase.Instance.FormatResourceDict(_reloadResourceUse));
		builder.AppendLine(
			_reloadResourceUse.Count > 0
				? $"    Reload time {_reloadTime}s, reload cost {reloadCost}/s"
				: $"    Reload time {_reloadTime}"
		);

		return builder.ToString();
	}
}
}