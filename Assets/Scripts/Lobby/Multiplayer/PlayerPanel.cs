using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.MultiplayerLobby
{
public class PlayerPanel : MonoBehaviour
{
	public Text PlayerName;
	public Text PlayerStatus;

	private Player _player;

	public void AssignPlayer(Player player)
	{
		_player = player;

		PlayerName.text = player.NickName;

		UpdateVehicleDisplay();
	}

	public void UpdateProps(Hashtable props)
	{
		if (props.ContainsKey(PropertyKeys.VEHICLE_NAME))
		{
			UpdateVehicleDisplay();
		}
	}

	private void UpdateVehicleDisplay()
	{
		object vehicleName = _player.CustomProperties[PropertyKeys.VEHICLE_NAME];

		PlayerStatus.text = vehicleName == null ? "Pondering what to use" : (string) vehicleName;
	}
}
}