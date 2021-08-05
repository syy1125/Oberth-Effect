﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class TurretedWeapon : MonoBehaviour, IWeaponSystem, IResourceConsumerBlock, ITooltipProvider
{
	private BlockCore _core;

	// From spec
	private float _rotationSpeed;
	private Transform _turretTransform;
	private List<IWeaponEffectEmitter> _weaponEmitters;

	// Cache
	private Dictionary<DamageType, float> _firepower;
	private Dictionary<string, float> _maxResourceUseRate;

	// State
	private bool _firing;
	private Vector2? _aimPoint;
	private float _turretAngle;
	private Dictionary<string, float> _resourceRequests;

	private void Awake()
	{
		_core = GetComponent<BlockCore>();

		_weaponEmitters = new List<IWeaponEffectEmitter>();
		_resourceRequests = new Dictionary<string, float>();
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IWeaponSystemRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	public void LoadSpec(TurretedWeaponSpec spec)
	{
		_rotationSpeed = spec.Turret.RotationSpeed;

		var turretObject = new GameObject("Turret");
		_turretTransform = turretObject.transform;
		_turretTransform.SetParent(transform);
		_turretTransform.localScale = Vector3.one;
		_turretTransform.localPosition = new Vector3(
			spec.Turret.TurretPivotOffset.x, spec.Turret.TurretPivotOffset.y, -1f
		);

		RendererHelper.AttachRenderers(_turretTransform, spec.Turret.Renderers);

		if (spec.ProjectileWeaponEffect != null)
		{
			var weaponEffectObject = new GameObject("ProjectileWeaponEffect");

			var weaponEffectTransform = weaponEffectObject.transform;
			weaponEffectTransform.SetParent(_turretTransform);
			weaponEffectTransform.localPosition = spec.ProjectileWeaponEffect.FiringPortOffset;
			weaponEffectTransform.localRotation = Quaternion.identity;

			var weaponEmitter = weaponEffectObject.AddComponent<ProjectileWeaponEffectEmitter>();
			weaponEmitter.LoadSpec(spec.ProjectileWeaponEffect);
			_weaponEmitters.Add(weaponEmitter);
		}
	}

	private void Start()
	{
		_turretAngle = 0;
		ApplyTurretRotation();
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IWeaponSystemRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
		ExecuteEvents.ExecuteHierarchy<IResourceConsumerBlockRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public int GetOwnerId() => _core.OwnerId;

	public void SetAimPoint(Vector2? aimPoint)
	{
		_aimPoint = aimPoint;
	}

	public void SetFiring(bool firing)
	{
		_firing = firing;
	}

	public IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest()
	{
		_resourceRequests.Clear();
		DictionaryUtils.SumDictionaries(
			_weaponEmitters
				.Select(emitter => emitter.GetResourceConsumptionRateRequest())
				.Where(dict => dict != null),
			_resourceRequests
		);
		return _resourceRequests;
	}

	public void SatisfyResourceRequestAtLevel(float level)
	{
		foreach (IWeaponEffectEmitter emitter in _weaponEmitters)
		{
			emitter.SatisfyResourceRequestAtLevel(level);
		}
	}

	private void FixedUpdate()
	{
		UpdateTurretRotationState();
		ApplyTurretRotation();

		foreach (IWeaponEffectEmitter emitter in _weaponEmitters)
		{
			emitter.EmitterFixedUpdate(_firing, _core.IsMine);
		}
	}

	private void UpdateTurretRotationState()
	{
		float targetAngle = _aimPoint == null
			? 0f
			: Vector3.SignedAngle(Vector3.up, transform.InverseTransformPoint(_aimPoint.Value), Vector3.forward);
		_turretAngle = Mathf.MoveTowardsAngle(_turretAngle, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
	}

	private void ApplyTurretRotation()
	{
		_turretTransform.localRotation = Quaternion.AngleAxis(_turretAngle, Vector3.forward);
	}

	public IReadOnlyDictionary<DamageType, float> GetMaxFirepower()
	{
		if (_firepower == null)
		{
			_firepower = new Dictionary<DamageType, float>();
			DictionaryUtils.SumDictionaries(
				_weaponEmitters.Select(emitter => emitter.GetMaxFirepower()),
				_firepower
			);
		}

		return _firepower;
	}

	public IReadOnlyDictionary<string, float> GetMaxResourceUseRate()
	{
		if (_maxResourceUseRate == null)
		{
			_maxResourceUseRate = new Dictionary<string, float>();

			DictionaryUtils.SumDictionaries(
				_weaponEmitters.Select(emitter => emitter.GetMaxResourceUseRate()), _maxResourceUseRate
			);
		}

		return _maxResourceUseRate;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder
			.AppendLine("Turreted Weapon")
			.AppendLine("  Turret")
			.AppendLine($"    Rotation speed {_rotationSpeed}°/s");

		foreach (IWeaponEffectEmitter emitter in _weaponEmitters)
		{
			builder.Append(emitter.GetEmitterTooltip());
		}

		IReadOnlyDictionary<DamageType, float> firepower = GetMaxFirepower();
		float maxDps = firepower.Values.Sum();
		builder.Append($"  Theoretical maximum DPS {maxDps:F1}");

		return builder.ToString();
	}
}
}