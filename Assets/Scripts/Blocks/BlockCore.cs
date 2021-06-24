using Photon.Pun;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IBlockCoreRegistry : IBlockRegistry<BlockCore>, IEventSystemHandler
{}

/// <remarks>
/// This interface is used by block scripts to execute effects on its destruction
/// </remarks>
internal interface IBlockDestructionEffect : IEventSystemHandler
{
	void OnDestroyedByDamage();
}

/// <remarks>
/// This interface is implemented by vehicle-level scripts to monitor block events
/// </remarks>
public interface IBlockLifecycleListener : IEventSystemHandler
{
	void OnBlockDestroyedByDamage(BlockCore blockCore);
}

/// <summary>
/// Controls core behaviour of a block.
/// For example, calculating physics and taking damage.
/// </summary>
[RequireComponent(typeof(BlockInfo))]
public class BlockCore : MonoBehaviour, IDamageable
{
	private BlockInfo _info;

	public int OwnerId { get; set; }
	public Vector2Int RootLocation { get; set; }
	public int Rotation { get; set; }
	public bool IsMine { get; private set; }

	public float Health { get; private set; }

	private void Awake()
	{
		_info = GetComponent<BlockInfo>();
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void Start()
	{
		Health = _info.MaxHealth;

		var photonView = GetComponentInParent<PhotonView>();
		IsMine = photonView == null || photonView.IsMine;
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public float GetDamageModifier(float armorPierce, DamageType damageType)
	{
		float armorModifier = Mathf.Min(armorPierce / _info.ArmorValue, 1f);
		return armorModifier;
	}

	public void DestroyByDamage()
	{
		if (!IsMine) return;

		Health = 0f;

		ExecuteEvents.Execute<IBlockDestructionEffect>(
			gameObject, null, (listener, _) => listener.OnDestroyedByDamage()
		);
		ExecuteEvents.ExecuteHierarchy<IBlockLifecycleListener>(
			gameObject, null, (listener, _) => listener.OnBlockDestroyedByDamage(this)
		);
		// Note that disabling of game object will be executed by VehicleCore
	}

	public void TakeDamage(float damage)
	{
		if (!IsMine) return;

		Health -= damage;
	}
}
}