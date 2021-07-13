using Photon.Pun;
using Syy1125.OberthEffect.Utils;
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
	public Vector2 CenterOfMassPosition => RootLocation + TransformUtils.RotatePoint(_info.CenterOfMass, Rotation);
	public bool IsMine { get; private set; }

	private float _health;
	public float HealthFraction => Mathf.Clamp01(_health / _info.MaxHealth);
	public bool IsDamaged => _info.MaxHealth - _health > Mathf.Epsilon;

	private Bounds _damageBounds;

	private void Awake()
	{
		_info = GetComponent<BlockInfo>();
		// BoundsInt has inclusive min and exclusive max.
		// To account for that behaviour, subtract 0.5 from the center when converting to float Bounds.  
		_damageBounds = new Bounds(_info.Bounds.center - new Vector3(0.5f, 0.5f, 0f), _info.Bounds.size);
	}

	private void OnEnable()
	{
		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void Start()
	{
		_health = _info.MaxHealth;

		var photonView = GetComponentInParent<PhotonView>();
		IsMine = photonView == null || photonView.IsMine;
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}

	public Bounds GetExplosionDamageBounds()
	{
		return _damageBounds;
	}

	public void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted)
	{
		float damageModifier = Mathf.Min(armorPierce / _info.ArmorValue, 1f);
		Debug.Assert(damageModifier > Mathf.Epsilon, "Damage modifier should not be zero");

		float effectiveDamage = damage * damageModifier;
		float effectiveHealth = _health / damageModifier;

		if (effectiveDamage < _health)
		{
			_health -= effectiveDamage;
			damage = 0f;
			damageExhausted = true;
		}
		else
		{
			_health = 0f;
			damage -= effectiveHealth;
			damageExhausted = false;

			ExecuteEvents.Execute<IBlockDestructionEffect>(
				gameObject, null, (listener, _) => listener.OnDestroyedByDamage()
			);
			ExecuteEvents.ExecuteHierarchy<IBlockLifecycleListener>(
				gameObject, null, (listener, _) => listener.OnBlockDestroyedByDamage(this)
			);
			// Note that, for multiplayer synchronization reasons, disabling of game object will be executed by VehicleCore
		}
	}
}
}