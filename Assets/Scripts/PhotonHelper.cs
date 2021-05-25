using ExitGames.Client.Photon;
using Photon.Pun;

namespace Syy1125.OberthEffect
{
public static class PhotonHelper
{
	public static void ClearPhotonPlayerProperties()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PropertyKeys.VEHICLE_NAME, null }, { PropertyKeys.READY, false } }
		);
	}
}
}