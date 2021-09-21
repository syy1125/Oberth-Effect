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
public class Shipyard : MonoBehaviourPun, IDamageable, IPunObservable
{
	public int TeamIndex;
	public float MaxHealth;
	public Transform[] SpawnPoints;

	public static readonly Dictionary<int, Shipyard> ActiveShipyards = new Dictionary<int, Shipyard>();

	public bool IsMine => PhotonNetwork.LocalPlayer.IsMasterClient;
	public int OwnerId => -1;

	private GameMode _gameMode;
	private bool _damageable;

	public float Health { get; private set; }

	private void Awake()
	{
		ActiveShipyards.Add(TeamIndex, this);
	}

	public static Shipyard GetShipyardForTeam(int teamIndex)
	{
		return ActiveShipyards.TryGetValue(teamIndex, out Shipyard shipyard) ? shipyard : null;
	}

	private void Start()
	{
		_gameMode = PhotonHelper.GetRoomGameMode();
		_damageable = _gameMode.CanDamageShipyards();
		Health = MaxHealth;

		Color teamColor = PhotonTeamManager.GetTeamColor(TeamIndex);
		foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
		{
			sprite.color = teamColor;
		}
	}

	private void OnDestroy()
	{
		ActiveShipyards.Remove(TeamIndex);
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
		if (!_damageable)
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
		else if (Health > 0f) // Shipyard isn't destroyed yet but will be by this attack
		{
			damage -= Health;
			Health = 0f;
			damageExhausted = false;

			AbstractGameManager.GameManager.OnShipyardDestroyed(TeamIndex);
		}
		else
		{
			damageExhausted = false;
		}
	}

	public void RequestBeamDamage(
		DamageType damageType, float damage, float armorPierce, int ownerId, Vector2 beamStart, Vector2 beamEnd
	)
	{
		photonView.RPC(nameof(TakeBeamDamage), photonView.Owner, damageType, damage, armorPierce);
	}

	// Shipyard is stationary, no need to re-do raycast on this end.
	[PunRPC]
	public void TakeBeamDamage(DamageType damageType, float damage, float armorPierce)
	{
		// Shipyard's dead and that's the most important part. In a 2-team game that's game over.
		// TODO in the future we could do overpenetration, especially if there are more than 2 teams.
		TakeDamage(damageType, ref damage, armorPierce, out bool _);
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(Health);
		}
		else
		{
			Health = (float) stream.ReceiveNext();
		}
	}
}
}