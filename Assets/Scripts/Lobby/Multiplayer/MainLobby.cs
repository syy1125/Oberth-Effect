using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class MainLobby : MonoBehaviourPunCallbacks
{
	[Header("Room List")]
	public Transform RoomListParent;
	public GameObject RoomPanelPrefab;
	public Text StatusIndicator;

	[Header("Player Controls")]
	public InputField PlayerNameInput;
	public Button JoinRoomButton;
	public Tooltip JoinRoomTooltip;
	public Button CreateRoomButton;
	public Tooltip CreateRoomTooltip;

	[Header("Room Screen")]
	public GameObject RoomScreen;

	private Dictionary<string, RoomInfo> _rooms;
	private Dictionary<string, GameObject> _roomPanels;
	private string _selectedRoom;

	private void Awake()
	{
		PhotonNetwork.AutomaticallySyncScene = true;

		_rooms = new Dictionary<string, RoomInfo>();
		_roomPanels = new Dictionary<string, GameObject>();

		string playerName = PlayerPrefs.GetString(PropertyKeys.PLAYER_NAME, "");
		PhotonNetwork.NickName = playerName;
		PlayerNameInput.text = playerName;
		PlayerNameInput.onValueChanged.AddListener(SetName);

		CreateRoomButton.onClick.AddListener(CreateRoom);
		JoinRoomButton.onClick.AddListener(JoinSelectedRoom);
	}

	public override void OnEnable()
	{
		base.OnEnable();

		if (!PhotonNetwork.IsConnectedAndReady)
		{
			PhotonNetwork.ConnectUsingSettings();
			StatusIndicator.text = "Connecting...";
		}
		else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
		{
			PhotonNetwork.JoinLobby();
			StatusIndicator.text = "Connecting...";
		}

		_selectedRoom = null;
		UpdateControls();
	}

	public override void OnConnectedToMaster()
	{
		if (!PhotonNetwork.InLobby)
		{
			PhotonNetwork.JoinLobby();
			StatusIndicator.text = "Connecting...";
		}

		UpdateControls();
	}

	public override void OnDisable()
	{
		base.OnDisable();
		PhotonNetwork.LeaveLobby();
	}

	public override void OnJoinedLobby()
	{
		_rooms.Clear();
		PhotonHelper.ClearPhotonPlayerProperties();

		RenderRoomList();
		UpdateControls();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		ApplyRoomListDeltas(roomList);
		RenderRoomList();
		UpdateControls();
	}

	private void ApplyRoomListDeltas(IEnumerable<RoomInfo> roomListDeltas)
	{
		foreach (RoomInfo room in roomListDeltas)
		{
			if (room.RemovedFromList)
			{
				if (_rooms.ContainsKey(room.Name))
				{
					_rooms.Remove(room.Name);
				}

				if (string.Equals(_selectedRoom, room.Name))
				{
					_selectedRoom = null;
				}
			}
			else if (_rooms.ContainsKey(room.Name))
			{
				_rooms[room.Name] = room;
			}
			else
			{
				_rooms.Add(room.Name, room);
			}
		}
	}

	private void RenderRoomList()
	{
		RoomInfo[] rooms = _rooms.Values
			.OrderBy(room => room.Name)
			.Where(room => room.IsOpen && room.IsVisible)
			.ToArray();
		KeyValuePair<string, GameObject>[] panels = _roomPanels
			.OrderBy(pair => pair.Key)
			.ToArray();

		int i = 0, j = 0;
		for (var siblingIndex = 0; i < rooms.Length && j < panels.Length; siblingIndex++)
		{
			int comparison = string.CompareOrdinal(rooms[i].Name, panels[j].Key);
			if (comparison == 0)
			{
				panels[j].Value.GetComponent<RoomPanel>().SetRoom(rooms[i]);

				i++;
				j++;
			}
			else if (comparison < 0) // Additional item in room list that's not on screen
			{
				GameObject panel = Instantiate(RoomPanelPrefab, RoomListParent);
				panel.GetComponent<RoomPanel>().SetRoom(rooms[i]);
				_roomPanels.Add(rooms[i].Name, panel);

				panel.transform.SetSiblingIndex(siblingIndex);
				i++;
			}
			else // comparison > 0, item currently on screen has been removed from list
			{
				Destroy(panels[j].Value);
				_roomPanels.Remove(panels[j].Key);

				if (string.Equals(_selectedRoom, panels[i].Key))
				{
					_selectedRoom = null;
					JoinRoomButton.interactable = false;
				}

				j++;
			}
		}

		for (; i < rooms.Length; i++)
		{
			GameObject panel = Instantiate(RoomPanelPrefab, RoomListParent);
			panel.GetComponent<RoomPanel>().SetRoom(rooms[i]);
			_roomPanels.Add(rooms[i].Name, panel);
		}

		for (; j < panels.Length; j++)
		{
			Destroy(panels[j].Value);
			_roomPanels.Remove(panels[j].Key);
		}

		if (_roomPanels.Count == 0)
		{
			StatusIndicator.gameObject.SetActive(true);
			StatusIndicator.text = "Nobody's here yet. Go start a game!";
		}
		else
		{
			StatusIndicator.gameObject.SetActive(false);
		}
	}

	public override void OnLeftLobby()
	{
		_rooms.Clear();
	}

	private void UpdateControls()
	{
		bool hasName = !string.IsNullOrWhiteSpace(PhotonNetwork.NickName);

		CreateRoomButton.interactable = PhotonNetwork.InLobby && hasName;
		CreateRoomTooltip.SetTooltip(
			!PhotonNetwork.InLobby
				? "Still connecting!"
				: !hasName
					? "You must enter a player name first."
					: null
		);

		JoinRoomButton.interactable = PhotonNetwork.InLobby && hasName && _selectedRoom != null;
		JoinRoomTooltip.SetTooltip(
			!PhotonNetwork.InLobby
				? "Still connecting!"
				: !hasName
					? "You must enter a player name first."
					: _selectedRoom == null
						? "First select a room to join."
						: null
		);
	}

	public void SelectRoom(string roomName)
	{
		if (_selectedRoom != null)
		{
			_roomPanels[_selectedRoom]?.GetComponent<RoomPanel>().SetSelected(false);
		}

		_selectedRoom = roomName;

		if (_selectedRoom != null)
		{
			_roomPanels[_selectedRoom]?.GetComponent<RoomPanel>().SetSelected(true);
		}

		UpdateControls();
	}

	private void SetName(string playerName)
	{
		PhotonNetwork.NickName = playerName;
		PlayerPrefs.SetString(PropertyKeys.PLAYER_NAME, playerName);

		UpdateControls();
	}

	private void CreateRoom()
	{
		PhotonNetwork.CreateRoom(
			Guid.NewGuid().ToString(),
			new RoomOptions
			{
				CustomRoomProperties = new Hashtable
				{
					{ PropertyKeys.ROOM_NAME, $"{PhotonNetwork.NickName}'s game" },
					{ PropertyKeys.GAME_MODE, (int) GameMode.Assault },
					{ PropertyKeys.FRIENDLY_FIRE_MODE, (int) FriendlyFireMode.Team }
				},
				CustomRoomPropertiesForLobby = new[] { PropertyKeys.ROOM_NAME, PropertyKeys.GAME_MODE }
			}
		);
	}

	private void JoinSelectedRoom()
	{
		PhotonNetwork.JoinRoom(_selectedRoom);
	}

	public override void OnJoinedRoom()
	{
		gameObject.SetActive(false);
		RoomScreen.SetActive(true);
	}

	public void ToMainMenu()
	{
		SceneManager.LoadScene("Scenes/Main Menu");
	}
}
}