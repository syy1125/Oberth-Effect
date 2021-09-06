using System.Text;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class PlayerPanel : MonoBehaviour
{
	public Text PlayerName;
	public Text PlayerStatus;

	public Image ReadyDisplay;
	public Color ReadyColor;
	public float FadeDuration;

	private Player _player;

	public void AssignPlayer(Player player)
	{
		_player = player;

		UpdateReadyDisplay();
		UpdateVehicleDisplay();
	}

	public void UpdateProps(Hashtable props)
	{
		bool vehicleChanged = props.ContainsKey(PropertyKeys.VEHICLE_NAME);
		bool readyChanged = props.ContainsKey(PropertyKeys.PLAYER_READY);

		if (vehicleChanged)
		{
			UpdateVehicleDisplay();
		}

		if (vehicleChanged || readyChanged)
		{
			UpdateReadyDisplay();
		}
	}

	private void UpdateVehicleDisplay()
	{
		object vehicleName = _player.CustomProperties[PropertyKeys.VEHICLE_NAME];

		PlayerStatus.text = vehicleName == null ? "Pondering what to use" : (string) vehicleName;
	}

	private void UpdateReadyDisplay()
	{
		bool ready = PhotonHelper.IsPlayerReady(_player);

		StringBuilder playerNameBuilder = new StringBuilder(_player.NickName);
		if (_player.IsMasterClient) playerNameBuilder.Append(" (Host)");
		if (!ready) playerNameBuilder.Append(" (Not Ready)");
		PlayerName.text = playerNameBuilder.ToString();

		ReadyDisplay.CrossFadeColor(
			ready ? ReadyColor : Color.white,
			FadeDuration, true, true
		);
	}
}
}