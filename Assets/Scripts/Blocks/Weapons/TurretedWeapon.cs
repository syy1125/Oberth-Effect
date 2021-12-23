using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class TurretedWeapon : AbstractWeapon, IHasDebrisState, ITooltipProvider
{
	public const string CLASS_KEY = "TurretedWeapon";

	private float _rotationSpeed;
	private Transform _turretTransform;
	private float _turretAngle;

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
			LoadProjectileWeapon(spec.ProjectileWeaponEffect);
		}
		else if (spec.BurstBeamWeaponEffect != null)
		{
			LoadBurstBeamWeapon(spec.BurstBeamWeaponEffect);
		}

		DefaultBinding = spec.DefaultBinding;
	}

	protected override void SetWeaponEffectTransform(GameObject weaponEffectObject, AbstractWeaponEffectSpec spec)
	{
		var weaponEffectTransform = weaponEffectObject.transform;
		weaponEffectTransform.SetParent(_turretTransform);
		weaponEffectTransform.localPosition = spec.FiringPortOffset;
		weaponEffectTransform.localRotation = Quaternion.identity;
	}

	private void Start()
	{
		_turretAngle = 0;
		ApplyTurretRotation();

		StartCoroutine(LateFixedUpdate());
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			UpdateTurretRotationState();
			ApplyTurretRotation();

			WeaponEmitter.EmitterFixedUpdate(Core.IsMine, Firing);

			yield return new WaitForFixedUpdate();
		}
	}

	private void UpdateTurretRotationState()
	{
		float targetAngle = AimPoint == null
			? 0f
			: Vector3.SignedAngle(Vector3.up, transform.InverseTransformPoint(AimPoint.Value), Vector3.forward);
		_turretAngle = Mathf.MoveTowardsAngle(_turretAngle, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
	}

	private void ApplyTurretRotation()
	{
		_turretTransform.localRotation = Quaternion.AngleAxis(_turretAngle, Vector3.forward);
	}

	public JObject SaveDebrisState()
	{
		return new JObject { { "TurretAngle", _turretAngle } };
	}

	public void LoadDebrisState(JObject state)
	{
		_turretAngle = state.Value<float>("TurretAngle");
		enabled = false;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder
			.AppendLine("Turreted Weapon")
			.AppendLine("  Turret")
			.AppendLine($"    Rotation speed {_rotationSpeed}°/s");

		builder.Append(WeaponEmitter.GetEmitterTooltip());

		AppendAggregateDamageInfo(builder);

		return builder.ToString();
	}
}
}