using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
[RequireComponent(typeof(BlockCore))]
public class TurretedProjectileWeapon : AbstractTurretedWeapon, ITooltipProvider
{
	[Header("Projectile")]
	public GameObject ProjectilePrefab;
	public BallisticProjectileConfig ProjectileConfig;
	public float ProjectileSpeed;

	public int ClusterCount = 1;
	public int BurstCount = 1;
	public float BurstInterval;

	protected override void Fire()
	{
		FireCluster();

		for (int i = 1; i < BurstCount; i++)
		{
			Invoke(nameof(FireCluster), BurstInterval * i);
		}
	}

	protected void FireCluster()
	{
		for (int i = 0; i < ClusterCount; i++)
		{
			float deviationAngle = GetDeviationAngle();

			GameObject projectile = PhotonNetwork.Instantiate(
				ProjectilePrefab.name, FiringPort.position, FiringPort.rotation,
				data: new object[]
					{ JsonUtility.ToJson(ProjectileConfig), JsonUtility.ToJson(ColorContext.ColorScheme) }
			);

			var projectileBody = projectile.GetComponent<Rigidbody2D>();
			projectileBody.velocity =
				Body.GetPointVelocity(FiringPort.position)
				+ (Vector2) FiringPort.TransformVector(Mathf.Sin(deviationAngle), Mathf.Cos(deviationAngle), 0f)
				* ProjectileSpeed;
		}

		if (UseRecoil)
		{
			Body.AddForceAtPosition(-FiringPort.up * ClusterRecoil, FiringPort.position, ForceMode2D.Impulse);
		}
	}

	private float GetDeviationAngle()
	{
		float deviationAngle = SpreadProfile switch
		{
			WeaponSpreadProfile.None => 0f,
			WeaponSpreadProfile.Gaussian => MathUtils.RandomGaussian() * SpreadAngle,
			WeaponSpreadProfile.Uniform => Random.Range(-SpreadAngle, SpreadAngle),
			_ => throw new ArgumentOutOfRangeException()
		};
		deviationAngle *= Mathf.Deg2Rad;
		return deviationAngle;
	}

	public override IReadOnlyDictionary<DamageType, float> GetMaxFirepower()
	{
		return new Dictionary<DamageType, float>
		{
			{ ProjectileConfig.DamageType, ProjectileConfig.Damage * ClusterCount * BurstCount / ReloadTime }
		};
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder.AppendLine("Turreted Projectile Weapon")
			.AppendLine("  Projectile")
			.AppendLine(
				ProjectileConfig.DamageType == DamageType.Explosive
					? $"    {ProjectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(ProjectileConfig.DamageType)} damage, {ProjectileConfig.ExplosionRadius * PhysicsConstants.METERS_PER_UNIT_LENGTH}m radius"
					: $"    {ProjectileConfig.Damage:F0} {DamageTypeUtils.GetColoredText(ProjectileConfig.DamageType)} damage, <color=\"lightblue\">{ProjectileConfig.ArmorPierce:0.#} AP</color>"
			)
			.AppendLine($"    Speed {ProjectileSpeed * PhysicsConstants.METERS_PER_UNIT_LENGTH:0.#}m/s")
			.AppendLine(
				$"    Max range {ProjectileSpeed * ProjectileConfig.Lifetime * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m"
			);

		string reloadCost = string.Join(
			" ", VehicleResourceDatabase.Instance.FormatResourceDict(ReloadResourceUse)
		);
		builder.AppendLine(
			ReloadResourceUse.Count > 0
				? $"    Reload time {ReloadTime}s, reload cost {reloadCost}"
				: $"    Reload time {ReloadTime}s"
		);

		builder.AppendLine("  Turret")
			.AppendLine($"    Rotation speed {RotateSpeed}°/s");

		if (ClusterCount > 1)
		{
			builder.AppendLine(
				BurstCount > 1
					? $"    {ClusterCount} shots per cluster, {BurstCount} clusters per burst, {BurstInterval}s between clusters in burst"
					: $"    {ClusterCount} shots per cluster"
			);
		}
		else if (BurstCount > 1)
		{
			builder.AppendLine($"    {BurstCount} shots per burst, {BurstInterval}s between shots in burst");
		}

		if (UseRecoil)
		{
			string shotOrCluster = ClusterCount > 1 ? "cluster" : "shot";
			builder.AppendLine(
				$"    Recoil {ClusterRecoil * PhysicsConstants.KN_PER_UNIT_FORCE:#,0.#}kN per {shotOrCluster}"
			);
		}

		switch (SpreadProfile)
		{
			case WeaponSpreadProfile.None:
				break;
			case WeaponSpreadProfile.Gaussian:
				builder.AppendLine($"    Gaussian spread ±{SpreadAngle}°");
				break;
			case WeaponSpreadProfile.Uniform:
				builder.AppendLine($"    Uniform spread ±{SpreadAngle}°");
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		float maxDps = ProjectileConfig.Damage * ClusterCount * BurstCount / ReloadTime;
		float ap = ProjectileConfig.DamageType == DamageType.Explosive ? 1f : ProjectileConfig.ArmorPierce;
		float minArmorModifier = Mathf.Min(ap / 10f, 1f);
		builder.AppendLine($"  Theoretical maximum DPS vs 1 armor {maxDps:F1}");
		builder.AppendLine($"  Theoretical maximum DPS vs 10 armor {maxDps * minArmorModifier:F1}");

		return builder.ToString();
	}
}
}