using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MultiplayerLobby
{
public class PlayerPanel : MonoBehaviour
{
	public Text PlayerName;
	public Text PlayerStatus;

	public void DisplayPlayer(Player player)
	{
		PlayerName.text = player.NickName;
		object vehicleName = player.CustomProperties[PhotonPropertyKeys.VEHICLE_NAME];
		PlayerStatus.text = vehicleName == null ? "Pondering what to use" : (string) vehicleName;
	}

	public void UpdateProps(Hashtable props)
	{
		if (props.ContainsKey(PhotonPropertyKeys.VEHICLE_NAME))
		{
			object vehicleName = props[PhotonPropertyKeys.VEHICLE_NAME];
			PlayerStatus.text = vehicleName == null ? "Pondering what to use" : (string) vehicleName;
		}
	}
}
}