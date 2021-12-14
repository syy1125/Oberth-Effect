using Photon.Realtime;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class RoomPanel : MonoBehaviour
{
	public Text RoomNameDisplay;
	public Color NormalColor;
	public Color SelectedColor;
	public float FadeDuration;

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
		_image.CrossFadeColor(NormalColor, 0f, true, true);
	}

	public void SetRoom(RoomInfo room)
	{
		_roomName = room.Name;
		RoomNameDisplay.text = (string) room.CustomProperties[PropertyKeys.ROOM_NAME];
	}

	private void HandleClick()
	{
		_lobby.SelectRoom(_roomName);
	}

	public void SetSelected(bool selected)
	{
		_image.CrossFadeColor(
			selected ? SelectedColor : NormalColor,
			FadeDuration, true, true
		);
	}
}
}