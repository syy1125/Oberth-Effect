using Photon.Pun;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IBlockCoreRegistry : IBlockRegistry<BlockCore>, IEventSystemHandler
{}

/// <summary>
/// Controls core behaviour of a block.
/// For example, calculating physics and taking damage.
/// </summary>
[RequireComponent(typeof(BlockInfo))]
public class BlockCore : MonoBehaviour, IDamageable
{
	private BlockInfo _info;

	public int OwnerId { get; set; }
	public bool IsMine { get; private set; }


	private bool _initialized;
	private bool _registered;
	public Vector2Int RootLocation { get; private set; }
	public int Rotation { get; private set; }

	public float Health { get; private set; }

	private void Awake()
	{
		_info = GetComponent<BlockInfo>();
	}

	private void OnEnable()
	{
		if (_initialized)
		{
			ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
				gameObject, null, (handler, _) => handler.RegisterBlock(this)
			);
			_registered = true;
		}
	}

	private void Start()
	{
		Health = _info.MaxHealth;

		var photonView = GetComponentInParent<PhotonView>();
		IsMine = photonView == null || photonView.IsMine;
	}

	private void OnDisable()
	{
		if (_registered)
		{
			ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
				gameObject, null, (handler, _) => handler.UnregisterBlock(this)
			);
		}
	}

	public void Initialize(Vector2Int rootLocation, int rotation)
	{
		RootLocation = rootLocation;
		Rotation = rotation;
		_initialized = true;

		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
		_registered = true;
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
		gameObject.SetActive(false);
	}

	public void TakeDamage(float damage)
	{
		if (!IsMine) return;

		Health -= damage;
	}
}
}