using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common;
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
	public Button ReadyButton;
	public Button StartGameButton;

	[Header("Lobby Screen")]
	public GameObject LobbyScreen;

	private SortedDictionary<int, GameObject> _playerPanels;
	private string _selectedVehicleName;
	private bool _ready;

	private static string _serializedVehicle;
	public static VehicleBlueprint SelectedVehicle { get; private set; }

	public static string SerializedSelectedVehicle
	{
		get => _serializedVehicle;
		private set
		{
			_serializedVehicle = value;
			SelectedVehicle = value == null ? null : JsonUtility.FromJson<VehicleBlueprint>(value);
		}
	}

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
		SerializedSelectedVehicle = null;
		VehicleList.OnSelectVehicle.AddListener(SelectVehicle);
		LoadVehicleButton.interactable = false;
		LoadVehicleButton.onClick.AddListener(LoadVehicleSelection);

		_ready = false;
		SelectVehicleButton.interactable = true;
		ReadyButton.interactable = false;
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

		SelectVehicleButton.onClick.RemoveListener(OpenVehicleSelection);
		ReadyButton.onClick.RemoveListener(ToggleReady);
		StartGameButton.onClick.RemoveListener(StartGame);

		foreach (GameObject go in _playerPanels.Values)
		{
			Destroy(go);
		}

		_playerPanels.Clear();
	}

	public override void OnRoomPropertiesUpdate(Hashtable nextProps)
	{
		if (nextProps.ContainsKey(PropertyKeys.ROOM_NAME))
		{
			RoomName.text = RoomNameInput.text = (string) nextProps[PropertyKeys.ROOM_NAME];
		}
	}

	private void SetRoomName(string roomName)
	{
		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PropertyKeys.ROOM_NAME, roomName } }
		);
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
	}

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable props)
	{
		_playerPanels[targetPlayer.ActorNumber].GetComponent<PlayerPanel>().UpdateProps(props);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
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

	private void UpdateMasterControls()
	{
		bool allReady = PhotonNetwork.CurrentRoom.Players.Values.All(
			player => player.IsMasterClient
				? player.CustomProperties.ContainsKey(PropertyKeys.VEHICLE_NAME)
				  && player.CustomProperties[PropertyKeys.VEHICLE_NAME] != null
				: player.CustomProperties.ContainsKey(PropertyKeys.READY)
				  && (bool) player.CustomProperties[PropertyKeys.READY]
		);

		SelectVehicleButton.interactable = true;
		StartGameButton.interactable = allReady;
	}

	private void UpdateClientControls()
	{
		SelectVehicleButton.interactable = !_ready;
		ReadyButton.interactable = SerializedSelectedVehicle != null;
		ReadyButton.GetComponentInChildren<Text>().text = _ready ? "Unready" : "Ready";
	}

	private void OpenVehicleSelection()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PropertyKeys.VEHICLE_NAME, null } }
		);
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
		SerializedSelectedVehicle = serializedVehicle;

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
		_ready = !_ready;
		PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PropertyKeys.READY, _ready } });

		UpdateClientControls();
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		PhotonHelper.ClearPhotonPlayerProperties();

		gameObject.SetActive(false);
		LobbyScreen.SetActive(true);
	}

	private static void StartGame()
	{
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.LoadLevel("Scenes/MP Test");
	}
}
}