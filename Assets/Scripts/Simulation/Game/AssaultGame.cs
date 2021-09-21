using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Game
{
public class AssaultGame : AbstractGameManager
{
	private List<int> _remainingTeams;
	private int? _winner;

	private void Awake()
	{
		if (PhotonHelper.GetRoomGameMode() == GameMode.Assault)
		{
			GameManager = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private void Start()
	{
		_remainingTeams = new List<int>(PhotonTeamManager.GetTeams());
	}

	private void OnDestroy()
	{
		if (GameManager == this)
		{
			GameManager = null;
		}
	}

	public override void OnShipyardDestroyed(int teamIndex)
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient) return;

		Debug.Log($"Team {teamIndex} shipyard destroyed, setting them as loss");
		_remainingTeams.Remove(teamIndex);
		photonView.RPC(nameof(SetDefeated), RpcTarget.All, teamIndex);

		if (_remainingTeams.Count == 1)
		{
			_winner = _remainingTeams[0];
			photonView.RPC(nameof(SetVictory), RpcTarget.All, _winner);
		}
		else if (_remainingTeams.Count == 0 && _winner == null)
		{
			// WTF?
			Debug.LogError("Remaining team count reached 0 without a winner");
			Debug.LogError($"Last team defeated is {teamIndex}");
		}
	}
}
}