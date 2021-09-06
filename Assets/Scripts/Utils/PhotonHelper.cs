using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Syy1125.OberthEffect.Utils
{
public static class PhotonHelper
{
	public static void ClearPhotonPlayerProperties()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PropertyKeys.VEHICLE_NAME, null }, { PropertyKeys.PLAYER_READY, false } }
		);
	}

	public static bool IsPlayerReady(Player player)
	{
		return player.IsMasterClient
			? player.CustomProperties.TryGetValue(PropertyKeys.VEHICLE_NAME, out object vehicleName)
			  && vehicleName != null
			: player.CustomProperties.TryGetValue(PropertyKeys.PLAYER_READY, out object ready)
			  && (bool) ready;
	}
}
}