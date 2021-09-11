using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
[RequireComponent(typeof(PhotonView))]
public class RoomScreen : MonoBehaviourPunCallbacks
{
	[Header("Player List")]
	public Text RoomName;

	public InputField RoomNameInput;

	public Transform PlayerListParent;
	public GameObject PlayerPanelPrefab;

	[Header("Controls")]
	public Button LoadVehicleButton;

	public GameObject VehicleSelectionScreen;
	public VehicleList VehicleList;

	public Button SelectVehicleButton;

	[Space]
	public SwitchSelect GameModeSelect;

	[Space]
	public Button ReadyButton;
	public Tooltip ReadyTooltip;
	public Button StartGameButton;
	public Tooltip StartGameTooltip;

	[Header("Lobby Screen")]
	public GameObject LobbyScreen;

	private SortedDictionary<int, GameObject> _playerPanels;
	private string _selectedVehicleName;

	private void Awake()
	{
		_playerPanels = new SortedDictionary<int, GameObject>();
	}

	public override void OnEnable()
	{
		base.OnEnable();

		RoomName.text = RoomNameInput.text =
			(string) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.ROOM_NAME];
		RoomNameInput.onEndEdit.AddListener(SetRoomName);

		_selectedVehicleName = null;
		VehicleSelection.SerializedVehicle = null;
		VehicleList.OnSelectVehicle.AddListener(SelectVehicle);
		LoadVehicleButton.interactable = false;
		LoadVehicleButton.onClick.AddListener(LoadVehicleSelection);

		GameModeSelect.SetOptions(EnumUtils.FormatNames(typeof(GameMode)));
		GameModeSelect.OnValueChanged.AddListener(SetGameMode);

		SelectVehicleButton.interactable = true;
		ReadyButton.interactable = false;
		ReadyTooltip.enabled = true;
		SelectVehicleButton.onClick.AddListener(OpenVehicleSelection);
		ReadyButton.onClick.AddListener(ToggleReady);
		StartGameButton.onClick.AddListener(StartGame);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UseMasterControls();
			UpdateMasterControls();
		}
		else
		{
			UseClientControls();
		}

		foreach (KeyValuePair<int, Player> pair in PhotonNetwork.CurrentRoom.Players)
		{
			GameObject go = Instantiate(PlayerPanelPrefab, PlayerListParent);
			go.GetComponent<PlayerPanel>().AssignPlayer(pair.Value);
			_playerPanels.Add(pair.Key, go);
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();

		RoomNameInput.onEndEdit.RemoveListener(SetRoomName);

		VehicleList.OnSelectVehicle.RemoveListener(SelectVehicle);
		LoadVehicleButton.onClick.RemoveListener(LoadVehicleSelection);

		GameModeSelect.OnValueChanged.RemoveListener(SetGameMode);

		SelectVehicleButton.onClick.RemoveListener(OpenVehicleSelection);
		ReadyButton.onClick.RemoveListener(ToggleReady);
		StartGameButton.onClick.RemoveListener(StartGame);

		foreach (GameObject go in _playerPanels.Values)
		{
			Destroy(go);
		}

		_playerPanels.Clear();
	}

	#region Photon Callbacks

	public override void OnRoomPropertiesUpdate(Hashtable nextProps)
	{
		if (nextProps.ContainsKey(PropertyKeys.ROOM_NAME))
		{
			RoomName.text = RoomNameInput.text = (string) nextProps[PropertyKeys.ROOM_NAME];
		}

		if (nextProps.ContainsKey(PropertyKeys.GAME_MODE))
		{
			if (!PhotonNetwork.LocalPlayer.IsMasterClient)
			{
				UpdateClientControls();
			}
		}

		if (nextProps.ContainsKey(PropertyKeys.TEAM_COLORS))
		{
			UpdatePlayerColors();
		}
	}

	private void SetRoomName(string roomName)
	{
		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PropertyKeys.ROOM_NAME, roomName } }
		);
	}

	private void SetGameMode(int gameMode)
	{
		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PropertyKeys.GAME_MODE, gameMode } }
		);
		UnreadyPlayers();
	}

	private static void UnreadyPlayers()
	{
		foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
		{
			if (!player.IsMasterClient)
			{
				player.SetCustomProperties(
					new Hashtable { { PropertyKeys.PLAYER_READY, false } }
				);
			}
		}
	}

	public override void OnPlayerEnteredRoom(Player player)
	{
		GameObject go = Instantiate(PlayerPanelPrefab, PlayerListParent);
		go.GetComponent<PlayerPanel>().AssignPlayer(player);
		_playerPanels.Add(player.ActorNumber, go);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
	}

	public override void OnPlayerLeftRoom(Player player)
	{
		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UseMasterControls();
			UpdateMasterControls();
		}
		else
		{
			UseClientControls();
		}

		Destroy(_playerPanels[player.ActorNumber]);
		_playerPanels.Remove(player.ActorNumber);
		
		foreach (GameObject panel in _playerPanels.Values)
		{
			panel.GetComponent<PlayerPanel>().UpdateKickButton();
		}
	}

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable props)
	{
		_playerPanels[targetPlayer.ActorNumber].GetComponent<PlayerPanel>().UpdatePlayerProps(props);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
		else
		{
			UpdateClientControls();
		}
	}

	#endregion

	#region Update Controls

	private void UseMasterControls()
	{
		RoomName.gameObject.SetActive(false);
		RoomNameInput.gameObject.SetActive(true);

		GameModeSelect.SetInteractable(true);

		ReadyButton.gameObject.SetActive(false);
		StartGameButton.gameObject.SetActive(true);
	}

	private void UseClientControls()
	{
		RoomName.gameObject.SetActive(true);
		RoomNameInput.gameObject.SetActive(false);

		GameModeSelect.SetInteractable(false);

		ReadyButton.gameObject.SetActive(true);
		StartGameButton.gameObject.SetActive(false);
	}

	private void UpdateMasterControls()
	{
		SelectVehicleButton.interactable = true;

		GameModeSelect.Value = (int) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.GAME_MODE];

		bool allReady = PhotonNetwork.CurrentRoom.Players.Values.All(PhotonHelper.IsPlayerReady);
		StartGameButton.interactable = allReady;
		StartGameTooltip.enabled = !allReady;
	}

	private void UpdateClientControls()
	{
		bool ready = PhotonHelper.IsPlayerReady(PhotonNetwork.LocalPlayer);
		SelectVehicleButton.interactable = !ready;

		GameModeSelect.Value = (int) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.GAME_MODE];

		ReadyButton.interactable = VehicleSelection.SerializedVehicle != null;
		ReadyButton.GetComponentInChildren<Text>().text = ready ? "Unready" : "Ready";
		ReadyTooltip.enabled = VehicleSelection.SerializedVehicle == null;
	}

	#endregion

	private void UpdatePlayerColors()
	{
		foreach (GameObject panel in _playerPanels.Values)
		{
			panel.GetComponent<PlayerPanel>().UpdateNameDisplay();
		}
	}

	private void OpenVehicleSelection()
	{
		VehicleSelectionScreen.SetActive(true);
	}

	private void SelectVehicle(string vehicleName)
	{
		_selectedVehicleName = vehicleName;
		if (vehicleName != null)
		{
			LoadVehicleButton.interactable = true;
		}
	}

	private void LoadVehicleSelection()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PropertyKeys.VEHICLE_NAME, _selectedVehicleName } }
		);

		string serializedVehicle = File.ReadAllText(VehicleList.ToVehiclePath(_selectedVehicleName));
		VehicleSelection.SerializedVehicle = serializedVehicle;

		VehicleSelectionScreen.SetActive(false);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
		else
		{
			UpdateClientControls();
		}
	}

	private void ToggleReady()
	{
		bool ready = PhotonHelper.IsPlayerReady(PhotonNetwork.LocalPlayer);
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PropertyKeys.PLAYER_READY, !ready } },
			new Hashtable { { PropertyKeys.PLAYER_READY, ready } }
		);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		VehicleSelection.SerializedVehicle = null;

		gameObject.SetActive(false);
		LobbyScreen.SetActive(true);
	}

	private static void StartGame()
	{
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.LoadLevel("Scenes/Multiplayer Game");
	}
}
}