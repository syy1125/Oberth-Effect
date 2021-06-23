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
	[HideInInspector]
	public Vector2Int RootLocation;

	public bool IsMine { get; private set; }
	public float Health { get; private set; }

	private void Awake()
	{
		_info = GetComponent<BlockInfo>();
	}

	private void Start()
	{
		Health = _info.MaxHealth;

		var photonView = GetComponentInParent<PhotonView>();
		IsMine = photonView == null || photonView.IsMine;
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