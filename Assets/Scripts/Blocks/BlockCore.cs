using Syy1125.OberthEffect.Blocks.Weapons;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
/// <summary>
/// Controls core behaviour of a block.
/// For example, calculating physics and taking damage.
/// </summary>
[RequireComponent(typeof(BlockInfo))]
public class BlockCore : MonoBehaviour
{
	private BlockInfo _info;

	[HideInInspector]
	public int OwnerId;

	public float Health { get; private set; }

	private void Awake()
	{
		_info = GetComponent<BlockInfo>();
	}

	private void Start()
	{
		Health = _info.MaxHealth;
	}

	public float GetDamageModifier(float armorPierce, DamageType damageType)
	{
		float armorModifier = Mathf.Min(armorPierce / _info.ArmorValue, 1f);
		return armorModifier;
	}

	public void DestroyBlock()
	{
		Health = 0f;
		gameObject.SetActive(false);
	}

	public void DamageBlock(float damage)
	{
		Health -= damage;
	}
}
}