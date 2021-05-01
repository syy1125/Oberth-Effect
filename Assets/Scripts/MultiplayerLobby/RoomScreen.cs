using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MultiplayerLobby
{
public class RoomScreen : MonoBehaviourPunCallbacks
{
	[Header("Player List")]
	public Text RoomName;

	public InputField RoomNameInput;

	public Transform PlayerListParent;
	public GameObject PlayerPanelPrefab;

	[Header("Controls")]
	public Button ReadyButton;

	public Button StartGameButton;
	public GameObject VehicleSelectionScreen;
	public Button LoadVehicleButton;

	[Header("Lobby Screen")]
	public GameObject LobbyScreen;

	private SortedDictionary<int, GameObject> _playerPanels;
	private string _selectedVehicle;

	private void Awake()
	{
		_playerPanels = new SortedDictionary<int, GameObject>();
	}

	public override void OnEnable()
	{
		base.OnEnable();

		RoomName.text = RoomNameInput.text =
			(string) PhotonNetwork.CurrentRoom.CustomProperties[PhotonPropertyKeys.ROOM_NAME];
		RoomNameInput.onEndEdit.AddListener(SetRoomName);

		_selectedVehicle = null;
		LoadVehicleButton.interactable = false;
		LoadVehicleButton.onClick.AddListener(LoadVehicleSelection);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UseMasterControls();
		}
		else
		{
			UseClientControls();
		}

		foreach (KeyValuePair<int, Player> pair in PhotonNetwork.CurrentRoom.Players)
		{
			GameObject go = Instantiate(PlayerPanelPrefab, PlayerListParent);
			go.GetComponent<PlayerPanel>().DisplayPlayer(pair.Value);
			_playerPanels.Add(pair.Key, go);
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();

		RoomNameInput.onEndEdit.RemoveListener(SetRoomName);

		LoadVehicleButton.onClick.RemoveListener(LoadVehicleSelection);

		foreach (GameObject go in _playerPanels.Values)
		{
			Destroy(go);
		}

		_playerPanels.Clear();
	}

	public override void OnRoomPropertiesUpdate(Hashtable nextProps)
	{
		if (nextProps.ContainsKey(PhotonPropertyKeys.ROOM_NAME))
		{
			RoomName.text = RoomNameInput.text = (string) nextProps[PhotonPropertyKeys.ROOM_NAME];
		}
	}

	private void SetRoomName(string roomName)
	{
		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PhotonPropertyKeys.ROOM_NAME, roomName } }
		);
	}

	public override void OnPlayerEnteredRoom(Player player)
	{
		GameObject go = Instantiate(PlayerPanelPrefab, PlayerListParent);
		go.GetComponent<PlayerPanel>().DisplayPlayer(player);
		_playerPanels.Add(player.ActorNumber, go);
	}

	public override void OnPlayerLeftRoom(Player player)
	{
		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UseMasterControls();
		}
		else
		{
			UseClientControls();
		}

		Destroy(_playerPanels[player.ActorNumber]);
		_playerPanels.Remove(player.ActorNumber);
	}

	public override void OnPlayerPropertiesUpdate(Player player, Hashtable props)
	{
		_playerPanels[player.ActorNumber].GetComponent<PlayerPanel>().UpdateProps(props);
	}

	private void UseMasterControls()
	{
		RoomName.gameObject.SetActive(false);
		RoomNameInput.gameObject.SetActive(true);
	}

	private void UseClientControls()
	{
		RoomName.gameObject.SetActive(true);
		RoomNameInput.gameObject.SetActive(false);
	}

	public void OpenVehicleSelection()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PhotonPropertyKeys.VEHICLE_NAME, null } });
		VehicleSelectionScreen.SetActive(true);
	}

	public void SelectVehicle(string vehicleName)
	{
		_selectedVehicle = vehicleName;
		if (vehicleName != null)
		{
			LoadVehicleButton.interactable = true;
		}
	}

	private void LoadVehicleSelection()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PhotonPropertyKeys.VEHICLE_NAME, _selectedVehicle } }
		);
		VehicleSelectionScreen.SetActive(false);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PhotonPropertyKeys.VEHICLE_NAME, null } });

		gameObject.SetActive(false);
		LobbyScreen.SetActive(true);
	}
}
}