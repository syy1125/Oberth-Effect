using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ColorContext))]
[RequireComponent(typeof(DamagingProjectile))]
public class NetworkedProjectileController : MonoBehaviourPun, IProjectileController, IPunInstantiateMagicCallback
{
	public bool IsMine => photonView.IsMine;
	public int OwnerId => photonView.OwnerActorNr;

	private PointDefenseTarget _pdTarget;
	private float _healthDamageScaling;
	private bool _despawning;

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		var config = ModLoader.Deserializer.Deserialize<NetworkedProjectileConfig>(
			CompressionUtils.Decompress((byte[]) instantiationData[0])
		);

		GetComponent<ColorContext>().SetColorScheme(config.ColorScheme);

		GetComponent<DamagingProjectile>().Init(
			config.Damage, config.DamageType, config.ArmorPierce, config.ExplosionRadius,
			null, config.Lifetime
		);

		GetComponent<BoxCollider2D>().size = config.ColliderSize;

		if (config.PointDefenseTarget != null)
		{
			_pdTarget = gameObject.AddComponent<PointDefenseTarget>();
			_pdTarget.Init(config.PointDefenseTarget, config.ColliderSize);
			_pdTarget.OnDestroyedByDamage.AddListener(RequestDestroyProjectile);
			_healthDamageScaling = config.HealthDamageScaling;

			gameObject.AddComponent<ReferenceFrameProvider>();
			var radiusProvider = gameObject.AddComponent<ConstantCollisionRadiusProvider>();
			radiusProvider.Radius = config.ColliderSize.magnitude / 2;
		}

		foreach (var entry in config.ProjectileComponents)
		{
			LoadModComponent(entry);
		}

		RendererHelper.AttachRenderers(transform, config.Renderers);

		if (config.TrailParticles != null && config.TrailParticles.Length > 0)
		{
			gameObject.AddComponent<ProjectileParticleTrail>().LoadTrailParticles(config.TrailParticles);
		}

		GetComponent<ColorSchemePainter>()?.ApplyColorScheme();

		foreach (var listener in GetComponents<IProjectileLifecycleListener>())
		{
			listener.AfterSpawn();
		}
	}

	private void LoadModComponent(KeyValuePair<string, object> entry)
	{
		var componentType = NetworkedProjectileConfig.GetComponentType(entry.Key);
		var specType = NetworkedProjectileConfig.GetSpecType(entry.Key);
		if (componentType == null)
		{
			Debug.LogError($"Failed to find component types for \"{entry.Key}\"");
			return;
		}

		if (!specType.IsInstanceOfType(entry.Value))
		{
			Debug.LogError($"Received spec under key \"{entry.Key}\" but it is not of type `{specType.FullName}`");
			return;
		}

		var component = gameObject.AddComponent(componentType);

		if (!typeof(INetworkedProjectileComponent<>).MakeGenericType(specType).IsAssignableFrom(componentType))
		{
			Debug.LogError(
				$"`{componentType.FullName}` does not implement `INetworkedProjectileComponent<{specType.FullName}>`. Skipping spec loading."
			);
			return;
		}

		componentType.GetMethod(nameof(INetworkedProjectileComponent<object>.LoadSpec))
			.Invoke(component, new[] { entry.Value });
	}

	#region Projectile RPC

	public void InvokeProjectileRpc(Type componentType, string methodName, Player target, params object[] parameters)
	{
		photonView.RPC(nameof(ReceiveProjectileRpc), target, componentType.ToString(), methodName, parameters);
	}

	public void InvokeProjectileRpc(Type componentType, string methodName, RpcTarget target, params object[] parameters)
	{
		photonView.RPC(nameof(ReceiveProjectileRpc), target, componentType.ToString(), methodName, parameters);
	}

	[PunRPC]
	private void ReceiveProjectileRpc(string componentType, string methodName, object[] parameters)
	{
		var component = GetComponent(componentType);
		if (component == null)
		{
			Debug.LogError($"Component of type `{componentType}` does not exist on projectile");
			return;
		}

		var method = component.GetType().GetMethod(
			methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
		);
		if (method == null)
		{
			Debug.LogError($"Method {methodName} does not exist for {component.GetType()}");
			return;
		}

		method.Invoke(component, parameters);
	}

	#endregion

	private float GetHealthDamageModifier()
	{
		return _pdTarget == null
			? 1f
			: MathUtils.Remap(_pdTarget.HealthFraction, 0f, 1f, 1f, 1f - _healthDamageScaling);
	}

	public void RequestDestroyProjectile()
	{
		if (!_despawning)
		{
			photonView.RPC(nameof(InvokeBeforeDespawn), RpcTarget.All);
			gameObject.SetActive(false);
		}

		PhotonNetwork.Destroy(gameObject);
	}

	[PunRPC]
	private void InvokeBeforeDespawn()
	{
		if (_despawning) return;
		_despawning = true;

		foreach (var listener in GetComponents<IProjectileLifecycleListener>())
		{
			listener.BeforeDespawn();
		}
	}
}
}