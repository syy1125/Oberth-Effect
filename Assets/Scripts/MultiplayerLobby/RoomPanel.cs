using System;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MultiplayerLobby
{
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class RoomPanel : MonoBehaviour
{
	public Text RoomNameDisplay;
	public ColorBlock Colors = ColorBlock.defaultColorBlock;

	private MainLobby _lobby;
	private Button _button;
	private Image _image;

	private string _roomName;

	private void Awake()
	{
		_lobby = GetComponentInParent<MainLobby>();

		_button = GetComponent<Button>();
		_button.onClick.AddListener(HandleClick);
		_image = GetComponent<Image>();
	}

	private void Start()
	{
		_image.CrossFadeColor(Colors.normalColor, 0f, true, true);
	}

	public void SetRoom(RoomInfo room)
	{
		_roomName = room.Name;
		RoomNameDisplay.text = (string) room.CustomProperties[PhotonPropertyKeys.ROOM_NAME];
	}

	private void HandleClick()
	{
		_lobby.SelectRoom(_roomName);
	}

	public void SetSelected(bool selected)
	{
		_image.CrossFadeColor(
			selected ? Colors.selectedColor : Colors.normalColor,
			Colors.fadeDuration, true, true
		);
	}
}
}