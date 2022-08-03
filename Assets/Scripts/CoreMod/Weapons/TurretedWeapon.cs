using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.CoreMod.Weapons.Launcher;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public struct TurretSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float RotationSpeed;
	public Vector2 TurretPivotOffset;
	public RendererSpec[] Renderers;
}

[CreateSchemaFile("TurretedWeaponSpecSchema")]
public class TurretedWeaponSpec : AbstractWeaponSpec
{
	public TurretSpec Turret;
}

public class TurretedWeapon : AbstractWeapon,
	IBlockComponent<TurretedWeaponSpec>,
	IHasDebrisState,
	ITooltipComponent
{
	public const string CLASS_KEY = "TurretedWeapon";

	private float _rotationSpeed;
	private Transform _turretTransform;
	private float _turretAngle;

	public void LoadSpec(TurretedWeaponSpec spec, in BlockContext context)
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

		if (spec.ProjectileLauncher != null)
		{
			LoadProjectileWeapon(spec.ProjectileLauncher, context);
		}
		else if (spec.BurstBeamLauncher != null)
		{
			LoadBurstBeamWeapon(spec.BurstBeamLauncher, context);
		}
		else if (spec.MissileLauncher != null)
		{
			LoadMissileWeapon(spec.MissileLauncher, context);
		}

		DefaultBinding = spec.DefaultBinding;
	}

	protected override void SetWeaponLauncherTransform(GameObject weaponEffectObject, AbstractWeaponLauncherSpec spec)
	{
		var weaponEffectTransform = weaponEffectObject.transform;
		weaponEffectTransform.SetParent(_turretTransform);
		weaponEffectTransform.localPosition = spec.FiringPortOffset;
		weaponEffectTransform.localRotation = Quaternion.identity;
		weaponEffectTransform.localScale = Vector3.one;
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

		while (isActiveAndEnabled)
		{
			UpdateTurretRotationState();
			ApplyTurretRotation();

			WeaponLauncher.LauncherFixedUpdate(Core.IsMine, Firing);

			yield return new WaitForFixedUpdate();
		}
	}

	private void UpdateTurretRotationState()
	{
		if (AimPoint == null)
		{
			_turretAngle = Mathf.MoveTowardsAngle(_turretAngle, 0f, _rotationSpeed * Time.fixedDeltaTime);
		}
		else
		{
			Vector2 localAimPoint = transform.InverseTransformPoint(AimPoint.Value) - _turretTransform.localPosition;
			float targetAngle = Vector3.SignedAngle(Vector3.up, localAimPoint, Vector3.forward);
			_turretAngle = Mathf.MoveTowardsAngle(_turretAngle, targetAngle, _rotationSpeed * Time.fixedDeltaTime);
		}
	}

	private void ApplyTurretRotation()
	{
		_turretTransform.localRotation = Quaternion.AngleAxis(_turretAngle, Vector3.forward);
	}

	public JObject SaveDebrisState()
	{
		return new() { { "TurretAngle", _turretAngle } };
	}

	public void LoadDebrisState(JObject state)
	{
		_turretAngle = state.Value<float>("TurretAngle");
		enabled = false;
	}

	public void GetTooltip(StringBuilder builder, string indent)
	{
		builder
			.AppendLine($"{indent}Turreted Weapon")
			.AppendLine($"{indent}  Turret")
			.AppendLine($"{indent}    Rotation speed {_rotationSpeed}°/s");

		WeaponLauncher.GetTooltip(builder, indent + "  ");

		AppendAggregateDamageInfo(builder, indent + "  ");
	}
}
}