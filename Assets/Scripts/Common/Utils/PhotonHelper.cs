using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Match;

namespace Syy1125.OberthEffect.Common.Utils
{
public static class PhotonHelper
{
	public static void ResetPhotonPlayerProperties()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable
			{
				{ PropertyKeys.VEHICLE_NAME, null },
				{ PropertyKeys.VEHICLE_COST, 0 },
				{ PropertyKeys.PLAYER_READY, false },
				{ PropertyKeys.TEAM_INDEX, -1 }
			}
		);
	}

	public static bool IsPlayerReady(Player player)
	{
		return player.IsMasterClient
			? player.CustomProperties[PropertyKeys.VEHICLE_NAME] != null
			: (bool) player.CustomProperties[PropertyKeys.PLAYER_READY];
	}

	public static GameMode GetRoomGameMode()
	{
		return PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyKeys.GAME_MODE, out object gameMode)
			? (GameMode) gameMode
			: GameMode.TestDrive;
	}

	public static int GetRoomCostLimit()
	{
		return PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(PropertyKeys.COST_LIMIT, out object costLimit)
			? (int) costLimit
			: 1000;
	}
}
}