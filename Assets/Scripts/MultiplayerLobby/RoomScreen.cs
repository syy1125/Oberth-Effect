using System;
using System.Collections.Generic;
using System.IO;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MultiplayerLobby
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

	public Button ReadyButton;
	public Button StartGameButton;

	[Header("Lobby Screen")]
	public GameObject LobbyScreen;

	private SortedDictionary<int, GameObject> _playerPanels;
	private string _selectedVehicle;

	private static Dictionary<string, VehicleBlueprint> _syncVehicles =
		new Dictionary<string, VehicleBlueprint>(); // Vehicles from other players

	public delegate void VehicleSynchronizedEvent(string id);

	public static VehicleSynchronizedEvent OnVehicleSynchronized;

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
		VehicleList.OnSelectVehicle.AddListener(SelectVehicle);
		LoadVehicleButton.interactable = false;
		LoadVehicleButton.onClick.AddListener(LoadVehicleSelection);

		// TODO ready/unready
		StartGameButton.onClick.AddListener(StartGame);

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

		StartGameButton.onClick.RemoveListener(StartGame);

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
		go.GetComponent<PlayerPanel>().AssignPlayer(player);
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

		ReadyButton.gameObject.SetActive(false);
		StartGameButton.gameObject.SetActive(true);
	}

	private void UseClientControls()
	{
		RoomName.gameObject.SetActive(true);
		RoomNameInput.gameObject.SetActive(false);

		ReadyButton.gameObject.SetActive(true);
		StartGameButton.gameObject.SetActive(false);
	}

	public void OpenVehicleSelection()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable
			{
				{ PhotonPropertyKeys.VEHICLE_ID, null },
				{ PhotonPropertyKeys.VEHICLE_NAME, null }
			}
		);
		VehicleSelectionScreen.SetActive(true);
	}

	private void SelectVehicle(string vehicleName)
	{
		_selectedVehicle = vehicleName;
		if (vehicleName != null)
		{
			LoadVehicleButton.interactable = true;
		}
	}

	private void LoadVehicleSelection()
	{
		var id = Guid.NewGuid().ToString();
		string serializedVehicle = File.ReadAllText(VehicleList.ToVehiclePath(_selectedVehicle));

		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable
			{
				{ PhotonPropertyKeys.VEHICLE_ID, id },
				{ PhotonPropertyKeys.VEHICLE_NAME, _selectedVehicle }
			}
		);
		photonView.RPC("StoreVehicle", RpcTarget.AllBuffered, id, serializedVehicle);

		VehicleSelectionScreen.SetActive(false);
	}

	[PunRPC]
	private void StoreVehicle(string id, string data)
	{
		_syncVehicles.Add(id, JsonUtility.FromJson<VehicleBlueprint>(data));
		OnVehicleSynchronized?.Invoke(id);
	}

	public static VehicleBlueprint GetVehicle(string id)
	{
		return _syncVehicles[id];
	}

	public static bool VehicleReady(string id)
	{
		return _syncVehicles.ContainsKey(id);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable
			{
				{ PhotonPropertyKeys.VEHICLE_ID, null },
				{ PhotonPropertyKeys.VEHICLE_NAME, null }
			}
		);

		_syncVehicles.Clear();

		gameObject.SetActive(false);
		LobbyScreen.SetActive(true);
	}

	private void StartGame()
	{
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.LoadLevel("Scenes/MP Test");
	}
}
}