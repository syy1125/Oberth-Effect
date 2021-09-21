using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Match;
using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class PhotonHelper
{
	public static void ResetPhotonPlayerProperties()
	{
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable
			{
				{ PropertyKeys.VEHICLE_NAME, null },
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
}
}