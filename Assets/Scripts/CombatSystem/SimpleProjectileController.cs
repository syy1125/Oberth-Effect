using System;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ColorContext))]
[RequireComponent(typeof(DamagingProjectile))]
public class SimpleProjectileController : MonoBehaviour, IProjectileController
{
	public int PlayerId;
	public int ProjectileId;

	public bool IsMine { get; set; }
	public int OwnerId => PlayerId;

	public void LoadConfig(SimpleProjectileConfig config)
	{
		GetComponent<ColorContext>().SetColorScheme(config.ColorScheme);

		GetComponent<DamagingProjectile>().Init(
			config.Damage, config.DamageType, config.ArmorPierce, config.ExplosionRadius,
			null, config.Lifetime
		);

		GetComponent<BoxCollider2D>().size = config.ColliderSize;

		RendererHelper.AttachRenderers(transform, config.Renderers);

		if (config.TrailParticles != null && config.TrailParticles.Length > 0)
		{
			gameObject.AddComponent<ProjectileParticleTrail>().LoadTrailParticles(config.TrailParticles);
		}
	}

	public void InvokeProjectileRpc(Type componentType, string methodName, Player target, params object[] parameters)
	{
		GetComponentInParent<SimpleProjectileManager>().InvokeProjectileRpc(
			PlayerId, ProjectileId, componentType, methodName, target, parameters
		);
	}

	public void InvokeProjectileRpc(Type componentType, string methodName, RpcTarget target, params object[] parameters)
	{
		GetComponentInParent<SimpleProjectileManager>().InvokeProjectileRpc(
			PlayerId, ProjectileId, componentType, methodName, target, parameters
		);
	}

	public void RequestDestroyProjectile()
	{
		GetComponentInParent<SimpleProjectileManager>().DestroyProjectile(this);
	}
}
}