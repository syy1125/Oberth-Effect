using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class RoomPlayerList : MonoBehaviourPunCallbacks
{
	public Transform PlayerListParent;
	public GameObject TeamPanelPrefab;
	public GameObject TeamPlayerPanelPrefab;
	public GameObject PlayerPanelPrefab;

	public GameObject TeamColorPicker;

	private SortedDictionary<int, GameObject> _teamPanels = new SortedDictionary<int, GameObject>();
	private SortedDictionary<int, GameObject> _playerPanels = new SortedDictionary<int, GameObject>();
	private bool _teamMode;

	public override void OnEnable()
	{
		base.OnEnable();

		_teamMode = PhotonHelper.GetRoomGameMode().IsTeamMode();

		if (_teamMode)
		{
			CreateTeamPlayerPanels();
		}
		else
		{
			CreateSoloPlayerPanels();
		}
	}

	private void CreateTeamPlayerPanels()
	{
		foreach (int teamIndex in PhotonTeamHelper.GetTeams())
		{
			GameObject panel = Instantiate(TeamPanelPrefab, PlayerListParent);
			panel.GetComponent<TeamPanel>().SetTeamIndex(teamIndex);
			panel.GetComponent<TeamPanel>().ColorPickerOverlay = TeamColorPicker;
			_teamPanels.Add(teamIndex, panel);
		}

		foreach (KeyValuePair<int, Player> pair in PhotonNetwork.CurrentRoom.Players)
		{
			int teamIndex = PhotonTeamHelper.GetPlayerTeamIndex(pair.Value);
			if (PhotonTeamHelper.IsValidTeam(teamIndex))
			{
				InstantiatePlayerPanel(pair.Value, TeamPlayerPanelPrefab, _teamPanels[teamIndex].transform);
			}
		}

		LayoutRebuilder.MarkLayoutForRebuild(PlayerListParent.GetComponent<RectTransform>());
	}

	private void CreateSoloPlayerPanels()
	{
		foreach (KeyValuePair<int, Player> pair in PhotonNetwork.CurrentRoom.Players)
		{
			InstantiatePlayerPanel(pair.Value, PlayerPanelPrefab, PlayerListParent);
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();

		foreach (GameObject go in _playerPanels.Values)
		{
			Destroy(go);
		}

		_playerPanels.Clear();
	}

	private void InstantiatePlayerPanel(Player player, GameObject prefab, Transform parent)
	{
		GameObject panel = Instantiate(prefab, parent);
		panel.GetComponent<PlayerPanel>().AssignPlayer(player);
		panel.GetComponent<DragPlayerPanel>()?.UpdateDraggable();
		_playerPanels.Add(player.ActorNumber, panel);
	}

	#region Photon Callbacks

	public override void OnPlayerEnteredRoom(Player player)
	{
		if (_teamMode)
		{
			int teamIndex = PhotonTeamHelper.GetPlayerTeamIndex(player);
			if (PhotonTeamHelper.IsValidTeam(teamIndex))
			{
				InstantiatePlayerPanel(player, TeamPlayerPanelPrefab, _teamPanels[teamIndex].transform);
			}
		}
		else
		{
			InstantiatePlayerPanel(player, PlayerPanelPrefab, PlayerListParent);
		}
	}

	public override void OnPlayerLeftRoom(Player player)
	{
		if (_playerPanels.ContainsKey(player.ActorNumber))
		{
			Destroy(_playerPanels[player.ActorNumber]);
			_playerPanels.Remove(player.ActorNumber);
		}

		foreach (GameObject panel in _playerPanels.Values)
		{
			panel.GetComponent<PlayerPanel>().UpdateNameDisplay();
			panel.GetComponent<PlayerPanel>().UpdateKickButton();
			panel.GetComponent<DragPlayerPanel>()?.UpdateDraggable();
		}
	}

	public override void OnRoomPropertiesUpdate(Hashtable props)
	{
		if (props.ContainsKey(PropertyKeys.GAME_MODE))
		{
			bool teamMode = PhotonHelper.GetRoomGameMode().IsTeamMode();

			if (teamMode != _teamMode)
			{
				_teamMode = teamMode;

				foreach (KeyValuePair<int, GameObject> pair in _teamPanels) Destroy(pair.Value);
				_teamPanels.Clear();
				foreach (KeyValuePair<int, GameObject> pair in _playerPanels) Destroy(pair.Value);
				_playerPanels.Clear();

				if (_teamMode)
				{
					CreateTeamPlayerPanels();
				}
				else
				{
					CreateSoloPlayerPanels();
				}

				// Everything has been destroyed and re-created, no need to carry out further updates.
				return;
			}
		}
	}

	public override void OnPlayerPropertiesUpdate(Player player, Hashtable props)
	{
		if (PhotonHelper.GetRoomGameMode().IsTeamMode() && props.ContainsKey(PropertyKeys.TEAM_INDEX))
		{
			int teamIndex = PhotonTeamHelper.GetPlayerTeamIndex(player);

			if (PhotonTeamHelper.IsValidTeam(teamIndex))
			{
				Transform teamPanel = _teamPanels[teamIndex].transform;

				if (_playerPanels.TryGetValue(player.ActorNumber, out GameObject panel))
				{
					if (!panel.transform.IsChildOf(teamPanel))
					{
						panel.transform.SetParent(teamPanel);
					}
				}
				else
				{
					InstantiatePlayerPanel(player, TeamPlayerPanelPrefab, teamPanel);
				}
			}
			else
			{
				if (_playerPanels.TryGetValue(player.ActorNumber, out GameObject panel))
				{
					Destroy(panel);
					_playerPanels.Remove(player.ActorNumber);
				}
			}
		}
	}

	#endregion
}
}