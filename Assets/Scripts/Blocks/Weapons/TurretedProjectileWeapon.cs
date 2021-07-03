using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public enum ClusterSpreadProfile
{
	None,
	Gaussian,
	Uniform
}

[RequireComponent(typeof(BlockCore))]
public class TurretedProjectileWeapon : MonoBehaviour, IResourceConsumerBlock, IWeaponSystem, ITooltipProvider
{
	[Header("References")]
	public GameObject ProjectilePrefab;
	public Transform Turret;
	public Transform FiringPort;

	[Header("Projectile Config")]
	public BallisticProjectileConfig ProjectileConfig;
	public float ProjectileSpeed;

	[Header("Weapon Config")]
	public float RotateSpeed;
	public float SpreadAngle = 0f;
	public ClusterSpreadProfile SpreadProfile;
	public int ClusterCount = 1;
	public int BurstCount = 1;
	public float BurstInterval;
	public bool UseRecoil;
	public float ClusterRecoil;
	public float ReloadTime; // Does NOT adjust for burst time
	public ResourceEntry[] ReloadResourceConsumptionRate;

	private Dictionary<VehicleResource, float> _resourceConsumption;

	private ColorContext _colorContext;
	private Rigidbody2D _body;
	private BlockCore _block;
	private bool _isMine;

	private Vector2? _aimPoint;
	private float _angle;
	private bool _firing;

	private float _reloadProgress;
	private float _resourceSatisfactionLevel;

	private void Awake()
	{
		_colorContext = GetComponentInParent<ColorContext>();
		_body = GetComponentInParent<Rigidbody2D>();
		_block = GetComponent<BlockCore>();
		_resourceConsumption = ReloadResourceConsumptionRate.ToDictionary(
			entry => entry.Resource, entry => entry.Amount
		);
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IWeaponSystemRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void Start()
	{
		_reloadProgress = ReloadTime;
		_aimPoint = null;
		_angle = 0;
		ApplyTurretRotation();

		var photonView = GetComponentInParent<PhotonView>();
		_isMine = photonView == null || photonView.IsMine;
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IWeaponSystemRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public IDictionary<VehicleResource, float> GetResourceConsumptionRateRequest()
	{
		return _reloadProgress >= ReloadTime ? null : _resourceConsumption;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		_resourceSatisfactionLevel = level;
	}

	public int GetOwnerId() => _block.OwnerId;

	public void SetAimPoint(Vector2? aimPoint)
	{
		_aimPoint = aimPoint;
	}

	public void SetFiring(bool firing)
	{
		_firing = firing;
	}

	private void FixedUpdate()
	{
		RotateTurret();

		if (_isMine)
		{
			if (_firing && _reloadProgress >= ReloadTime)
			{
				_reloadProgress -= ReloadTime;

				FireCluster();

				for (int i = 1; i < BurstCount; i++)
				{
					Invoke(nameof(FireCluster), BurstInterval * i);
				}
			}

			if (_reloadProgress < ReloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * _resourceSatisfactionLevel;
			}
		}
	}

	private void FireCluster()
	{
		for (int i = 0; i < ClusterCount; i++)
		{
			float deviationAngle = SpreadProfile switch
			{
				ClusterSpreadProfile.None => 0f,
				ClusterSpreadProfile.Gaussian => RandomNumberUtils.NextGaussian() * SpreadAngle,
				ClusterSpreadProfile.Uniform => Random.Range(-SpreadAngle, SpreadAngle),
				_ => throw new ArgumentOutOfRangeException()
			};
			deviationAngle *= Mathf.Deg2Rad;

			GameObject projectile = PhotonNetwork.Instantiate(
				ProjectilePrefab.name, FiringPort.position, FiringPort.rotation,
				data: new object[]
					{ JsonUtility.ToJson(ProjectileConfig), JsonUtility.ToJson(_colorContext.ColorScheme) }
			);

			var projectileBody = projectile.GetComponent<Rigidbody2D>();
			projectileBody.velocity =
				FiringPort.TransformVector(Mathf.Sin(deviationAngle), Mathf.Cos(deviationAngle), 0f) * ProjectileSpeed;
		}

		if (UseRecoil)
		{
			_body.AddForceAtPosition(-FiringPort.up * ClusterRecoil, FiringPort.position, ForceMode2D.Impulse);
		}
	}

	private void RotateTurret()
	{
		float targetAngle = _aimPoint == null
			? 0f
			: Vector3.SignedAngle(
				Vector3.up, transform.InverseTransformPoint(_aimPoint.Value), Vector3.forward
			);

		_angle = Mathf.MoveTowardsAngle(_angle, targetAngle, RotateSpeed * Time.fixedDeltaTime);

		ApplyTurretRotation();
	}

	private void ApplyTurretRotation()
	{
		Turret.localRotation = Quaternion.AngleAxis(_angle, Vector3.forward);
	}

	public string GetTooltip()
	{
		float maxDps = ProjectileConfig.Damage * ClusterCount * BurstCount / ReloadTime;

		List<string> lines = new List<string>
		{
			"Turreted Projectile Weapon",
			"  Projectile",
			$"    <color=\"red\">{ProjectileConfig.Damage:F0} damage</color>, <color=\"lightblue\">{ProjectileConfig.ArmorPierce:F0} AP</color>",
			$"    Speed {ProjectileSpeed * GamePhysicsConstants.METERS_PER_UNIT:F1} m/s",
			"  Turret",
			$"    Rotation speed {RotateSpeed}°/s",
		};

		if (ClusterCount > 1)
		{
			lines.Add(
				BurstCount > 1
					? $"    {ClusterCount} shots per cluster, {BurstCount} clusters per burst, {BurstInterval}s between clusters in burst"
					: $"    {ClusterCount} shots per cluster"
			);
		}
		else if (BurstCount > 1)
		{
			lines.Add(
				$"    {BurstCount} shots per burst, {BurstInterval}s between shots in burst"
			);
		}

		if (UseRecoil)
		{
			string shotOrCluster = ClusterCount > 1 ? "cluster" : "shot";
			lines.Add($"    Recoil {ClusterRecoil}kNs per {shotOrCluster}");
		}

		switch (SpreadProfile)
		{
			case ClusterSpreadProfile.None:
				break;
			case ClusterSpreadProfile.Gaussian:
				lines.Add($"    Gaussian spread ±{SpreadAngle}°");
				break;
			case ClusterSpreadProfile.Uniform:
				lines.Add($"    Uniform spread ±{SpreadAngle}°");
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		string reloadCost = string.Join(
			" ", ReloadResourceConsumptionRate.Select(entry => $"{entry.RichTextColoredEntry()}/s")
		);
		lines.Add(
			ReloadResourceConsumptionRate.Length > 0
				? $"    Reload time {ReloadTime}s, reload cost {reloadCost}"
				: $"    Reload time {ReloadTime}s"
		);

		lines.Add($"  Theoretical maximum DPS {maxDps:F1}");

		return string.Join("\n", lines);
	}
}
}