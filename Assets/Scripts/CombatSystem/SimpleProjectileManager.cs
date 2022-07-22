using System;
using System.Collections.Generic;
using System.Reflection;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Components.Singleton;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
[Serializable]
public struct SimpleProjectileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamageType DamageType;
	public float ArmorPierce; // Note that explosive damage will always have armor pierce of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;

	public ColorScheme ColorScheme;
	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] TrailParticles;
}

[RequireComponent(typeof(PhotonView))]
public class SimpleProjectileManager : SceneSingletonBehaviourPun<SimpleProjectileManager>
{
	public static SimpleProjectileManager Instance { get; private set; }

	public GameObject SimpleProjectilePrefab;

	#region Projectile Management

	/// <summary>
	/// Maps the player ID to a list of ballistic projectiles belonging to that player.
	/// Note that the index of the projectile in its list serves as its ID.
	/// Projectiles may be recycled. In that case, they are set to disabled and kept in the list.
	/// </summary>
	private Dictionary<int, List<GameObject>> _projectiles = new();
	// These are IDs from our destroyed projectiles that can be reused.
	private Queue<int> _recycledIds = new();

	public void CreateProjectile(
		Vector3 position, Quaternion rotation, Vector2 velocity, SimpleProjectileConfig config
	)
	{
		int playerId = PhotonNetwork.LocalPlayer.ActorNumber;

		if (!_projectiles.TryGetValue(playerId, out var playerProjectiles))
		{
			playerProjectiles = new();
			_projectiles.Add(playerId, playerProjectiles);
		}

		// If a recycled ID exists, use that. Otherwise, it's a new projectile.
		int projectileId = _recycledIds.TryDequeue(out int id) ? id : playerProjectiles.Count;

		photonView.RPC(
			nameof(ReceiveCreateProjectileRpc), RpcTarget.All,
			playerId, projectileId, PhotonNetwork.Time,
			position, rotation, velocity, CompressionUtils.Compress(JsonUtility.ToJson(config))
		);
	}

	[PunRPC]
	private void ReceiveCreateProjectileRpc(
		int playerId, int projectileId, double createTime,
		Vector3 position, Quaternion rotation, Vector2 velocity, byte[] configData
	)
	{
		if (!_projectiles.TryGetValue(playerId, out var playerProjectiles))
		{
			playerProjectiles = new();
			_projectiles.Add(playerId, playerProjectiles);
		}

		while (playerProjectiles.Count <= projectileId) playerProjectiles.Add(null);

		double lag = (PhotonNetwork.Time + 4294967.295 - createTime) % 4294967.295;
		Vector3 spawnPosition = position + (Vector3) velocity * (float) lag;
		var config = JsonUtility.FromJson<SimpleProjectileConfig>(CompressionUtils.Decompress(configData));

		if (playerProjectiles[projectileId] == null)
		{
			// New projectile
			GameObject projectile = Instantiate(SimpleProjectilePrefab, spawnPosition, rotation, transform);

			projectile.GetComponent<Rigidbody2D>().velocity = velocity;

			var controller = projectile.GetComponent<SimpleProjectileController>();
			controller.PlayerId = playerId;
			controller.ProjectileId = projectileId;
			controller.IsMine = playerId == PhotonNetwork.LocalPlayer.ActorNumber;
			controller.LoadConfig(config);

			playerProjectiles[projectileId] = projectile;

			foreach (var listener in projectile.GetComponents<IProjectileLifecycleListener>())
			{
				listener.AfterSpawn();
			}
		}
		else
		{
			// Recycled projectile
			var projectile = playerProjectiles[projectileId];
			projectile.SetActive(true);

			projectile.transform.position = spawnPosition;
			projectile.GetComponent<Rigidbody2D>().velocity = velocity;
			projectile.GetComponent<SimpleProjectileController>().LoadConfig(config);

			foreach (var listener in projectile.GetComponents<IProjectileLifecycleListener>())
			{
				listener.AfterSpawn();
			}
		}
	}

	public void DestroyProjectile(SimpleProjectileController projectile)
	{
		photonView.RPC(
			nameof(ReceiveDestroyProjectileRpc), RpcTarget.All, projectile.PlayerId, projectile.ProjectileId
		);
	}

	[PunRPC]
	private void ReceiveDestroyProjectileRpc(int playerId, int projectileId)
	{
		if (!_projectiles.TryGetValue(playerId, out var playerProjectiles)
		    || playerProjectiles.Count <= projectileId
		    || playerProjectiles[projectileId] == null)
		{
			Debug.LogError($"No projectile with id ({playerId},{projectileId})");
			return;
		}

		var projectile = playerProjectiles[projectileId];

		// If the projectile has already been despawned, don't do it again.
		if (!projectile.activeSelf) return;

		foreach (var listener in projectile.GetComponents<IProjectileLifecycleListener>())
		{
			listener.BeforeDespawn();
		}

		projectile.SetActive(false);
		projectile.transform.position = Vector3.zero;

		if (playerId == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			_recycledIds.Enqueue(projectileId);
		}
	}

	#endregion

	#region Projectile RPC

	public void InvokeProjectileRpc(
		int playerId, int projectileId, Type componentType, string methodName, Player target,
		object[] parameters
	)
	{
		photonView.RPC(
			nameof(ReceiveProjectileRpc), target,
			playerId, projectileId, componentType.ToString(), methodName, parameters
		);
	}

	public void InvokeProjectileRpc(
		int playerId, int projectileId, Type componentType, string methodName, RpcTarget target,
		object[] parameters
	)
	{
		photonView.RPC(
			nameof(ReceiveProjectileRpc), target,
			playerId, projectileId, componentType.ToString(), methodName, parameters
		);
	}

	[PunRPC]
	private void ReceiveProjectileRpc(
		int playerId, int projectileId, string componentType, string methodName, object[] parameters
	)
	{
		if (!_projectiles.TryGetValue(playerId, out var playerProjectiles)
		    || playerProjectiles.Count <= projectileId
		    || playerProjectiles[projectileId] == null)
		{
			Debug.LogError($"No projectile with id ({playerId},{projectileId})");
			return;
		}

		var projectile = playerProjectiles[projectileId];

		if (!projectile.activeSelf)
		{
			Debug.LogWarning($"Cannot invoke RPC `{componentType}`.`{methodName}` on a destroyed projectile");
			return;
		}

		var component = projectile.GetComponent(componentType);
		if (component == null)
		{
			Debug.LogError(
				$"Component of type `{componentType}` does not exist on projectile with ID ({playerId},{projectileId})"
			);
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
}
}