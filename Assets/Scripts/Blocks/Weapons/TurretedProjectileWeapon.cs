﻿using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class TurretedProjectileWeapon : MonoBehaviour, IResourceConsumerBlock, IWeaponSystem
{
	public GameObject WeaponPrefab;

	public int BurstCount = 1;
	public float BurstInterval;
	public float ReloadTime;
	public ResourceEntry[] ReloadResourceConsumptionRate;

	public Transform Turret;

	private Dictionary<VehicleResource, float> _resourceConsumption;

	private Vector2 _aimPoint;
	private bool _firing;

	private float _burstIndex;
	private float _burstCooldown;
	private float _reloadProgress;
	private float _resourceSatisfactionLevel;

	private void Awake()
	{
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
		_aimPoint = transform.TransformPoint(Vector3.up);
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

	public int GetOwnerId()
	{
		return 0;
	}

	public void SetAimPoint(Vector2 aimPoint)
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

		if (_firing && _reloadProgress >= ReloadTime)
		{
			_reloadProgress -= ReloadTime;
			Debug.Log("Weapon firing");
		}

		if (_reloadProgress < ReloadTime)
		{
			_reloadProgress += Time.fixedDeltaTime * _resourceSatisfactionLevel;
		}
	}

	private void RotateTurret()
	{
		float targetAngle = Vector3.SignedAngle(
			Vector3.up, transform.InverseTransformPoint(_aimPoint), Vector3.forward
		);
		Turret.rotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
	}
}
}