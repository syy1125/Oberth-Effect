using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Syy1125.OberthEffect.MultiplayerLobby
{
public class MainLobby : MonoBehaviourPunCallbacks
{
	[Header("Room List")]
	public Transform RoomListParent;

	public GameObject RoomPanelPrefab;

	private Dictionary<string, RoomInfo> _rooms;
	private Dictionary<string, GameObject> _roomPanels;
	private string _selectedRoom;

	private void Awake()
	{
		if (!PhotonNetwork.IsConnected)
		{
			PhotonNetwork.ConnectUsingSettings();
		}

		PhotonNetwork.AutomaticallySyncScene = true;

		_rooms = new Dictionary<string, RoomInfo>();
		_roomPanels = new Dictionary<string, GameObject>();
		_selectedRoom = null;
	}

	public override void OnConnectedToMaster()
	{
		if (!PhotonNetwork.InLobby)
		{
			PhotonNetwork.JoinLobby();
		}
	}

	public override void OnJoinedLobby()
	{
		_rooms.Clear();
		RenderRoomList();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		foreach (RoomInfo room in roomList)
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

		RenderRoomList();
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
	}

	public override void OnLeftLobby()
	{
		_rooms.Clear();
	}

	public void SelectRoom(string roomName)
	{
		if (_selectedRoom != null)
		{
			_roomPanels[_selectedRoom]?.GetComponent<RoomPanel>().SetSelected(false);
		}

		_selectedRoom = roomName;
	}
}
}