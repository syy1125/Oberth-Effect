using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Utils;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Game
{
public class Shipyard : MonoBehaviourPun, IDamageable
{
	public int TeamIndex;
	public float MaxHealth;
	public Transform[] SpawnPoints;

	private static readonly Dictionary<int, Shipyard> shipyards = new Dictionary<int, Shipyard>();

	public bool IsMine => PhotonNetwork.LocalPlayer.IsMasterClient;
	public int OwnerId => -1;

	private GameMode _gameMode;

	public float Health { get; private set; }

	private ContactFilter2D _beamRaycastFilter;
	private List<RaycastHit2D> _beamRaycastHits;

	private void Awake()
	{
		shipyards.Add(TeamIndex, this);
	}

	public static Shipyard GetShipyardForTeam(int teamIndex)
	{
		return shipyards.TryGetValue(teamIndex, out Shipyard shipyard) ? shipyard : null;
	}

	private void Start()
	{
		_gameMode = PhotonHelper.GetRoomGameMode();
		Health = MaxHealth;

		_beamRaycastFilter = new ContactFilter2D
		{
			layerMask = WeaponConstants.HIT_LAYER_MASK,
			useLayerMask = true
		};
		_beamRaycastHits = new List<RaycastHit2D>();
	}

	public Transform GetBestSpawnPoint(int playerIndex)
	{
		// Decreasing radius to try to ensure maximum free space before spawning
		foreach (var radius in new[] { 15f, 10f, 5f })
		{
			for (int i = 0; i < SpawnPoints.Length; i++)
			{
				Transform t = SpawnPoints[(playerIndex + i) % SpawnPoints.Length];
				// If there are no other crafts obstructing the spawn point, then it's viable.
				var overlapCollider = Physics2D.OverlapCircle(t.position, radius, WeaponConstants.HIT_LAYER_MASK);
				if (overlapCollider == null)
				{
					return t;
				}
			}
		}

		// Fallback
		return SpawnPoints[playerIndex % SpawnPoints.Length];
	}

	public Tuple<Vector2, Vector2> GetExplosionDamageBounds()
	{
		return new Tuple<Vector2, Vector2>(new Vector2(-1f, -1f), new Vector2(1f, 1f));
	}

	public void TakeDamage(DamageType damageType, ref float damage, float armorPierce, out bool damageExhausted)
	{
		if (!_gameMode.CanDamageShipyards())
		{
			damageExhausted = true;
			return;
		}

		if (damage < Health)
		{
			Health -= damage;
			damage = 0f;
			damageExhausted = true;
		}
		else
		{
			damage -= Health;
			Health = 0f;
			damageExhausted = false;

			Debug.Log($"Team {TeamIndex} shipyard destroyed");
		}
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		photonView.RPC(nameof(TakeBeamDamage), photonView.Owner, damageType, damage, armorPierce);
	}

	[PunRPC]
	public void TakeBeamDamage(DamageType damageType, float damage, float armorPierce)
	{
		// Shipyard's dead and that's the most important part. In a 2-team game that's game over.
		// TODO in the future we could do overpenetration, especially if there are more than 2 teams.
		TakeDamage(damageType, ref damage, armorPierce, out bool _);
	}
}
}