using Photon.Pun;
using Syy1125.OberthEffect.Components.Singleton;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
public class NetworkedProjectileManager : SceneSingletonBehaviour
{
	public static NetworkedProjectileManager Instance { get; private set; }

	public GameObject NetworkedProjectilePrefab;

	public GameObject CreateProjectile(
		Vector3 position, Quaternion rotation, Vector2 velocity, NetworkedProjectileConfig config
	)
	{
		GameObject projectile = PhotonNetwork.Instantiate(
			NetworkedProjectilePrefab.name, position, rotation,
			data: new object[]
			{
				// Use YAML serialization as the tag system can preserve type information
				CompressionUtils.Compress(ModLoader.Serializer.Serialize(config))
			}
		);

		projectile.GetComponent<Rigidbody2D>().velocity = velocity;

		return projectile;
	}
}
}