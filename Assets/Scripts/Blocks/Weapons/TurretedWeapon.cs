using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public abstract class TurretedWeapon : MonoBehaviour, IResourceConsumerBlock, IWeaponSystem
{
	[Header("References")]
	public Transform Turret;
	public Transform FiringPort;

	[Header("Weapon Config")]
	public float RotateSpeed;
	public float SpreadAngle = 0f;
	public WeaponSpreadProfile SpreadProfile;
	public bool UseRecoil;
	public float ClusterRecoil;

	public float ReloadTime; // Does NOT adjust for burst time
	public ResourceEntry[] ReloadResourceConsumptionRate;

	protected ColorContext ColorContext;
	protected Rigidbody2D Body;

	private BlockCore _block;
	private Dictionary<VehicleResource, float> _resourceConsumption;
	private bool _isMine;
	private Vector2? _aimPoint;
	private float _angle;
	private float _reloadProgress;
	private bool _firing;
	private float _resourceSatisfactionLevel;

	#region Unity Lifecycle

	private void Awake()
	{
		ColorContext = GetComponentInParent<ColorContext>();
		Body = GetComponentInParent<Rigidbody2D>();
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

	#endregion

	#region Block Functionality

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

				Fire();
			}

			if (_reloadProgress < ReloadTime)
			{
				_reloadProgress += Time.fixedDeltaTime * _resourceSatisfactionLevel;
			}
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

	protected abstract void Fire();

	#endregion

	#region User Interface

	public Dictionary<VehicleResource, float> GetMaxResourceUseRate()
	{
		return _resourceConsumption;
	}

	public abstract Dictionary<DamageType, float> GetDamageRatePotential();

	#endregion
}
}