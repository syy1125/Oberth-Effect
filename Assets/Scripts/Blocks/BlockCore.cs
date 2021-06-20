using Photon.Pun;
using Syy1125.OberthEffect.Blocks.Weapons;
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
public class BlockCore : MonoBehaviour
{
	private BlockInfo _info;

	private bool _isMine;

	[HideInInspector]
	public int OwnerId;
	[HideInInspector]
	public Vector2Int RootLocation;

	public float Health { get; private set; }

	private void Awake()
	{
		_info = GetComponent<BlockInfo>();
	}

	private void Start()
	{
		Health = _info.MaxHealth;

		var photonView = GetComponentInParent<PhotonView>();
		_isMine = photonView == null || photonView.IsMine;
	}

	public float GetDamageModifier(float armorPierce, DamageType damageType)
	{
		float armorModifier = Mathf.Min(armorPierce / _info.ArmorValue, 1f);
		return armorModifier;
	}

	public void DestroyBlock()
	{
		if (!_isMine) return;

		Health = 0f;
		gameObject.SetActive(false);
	}

	public void DamageBlock(float damage)
	{
		if (!_isMine) return;

		Health -= damage;
	}
}
}