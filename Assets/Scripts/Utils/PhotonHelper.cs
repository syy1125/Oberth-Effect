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

	public static int GetPlayerTeamIndex(Player player)
	{
		GameMode gameMode = GetRoomGameMode();

		if (gameMode.IsTeamMode())
		{
			return (int) player.CustomProperties[PropertyKeys.TEAM_INDEX];
		}
		else
		{
			return player.ActorNumber;
		}
	}

	public static Color GetPlayerTeamColor(Player player)
	{
		return GetPlayerTeamColor(player, Color.white);
	}

	public static Color GetPlayerTeamColor(Player player, Color fallback)
	{
		if (PhotonNetwork.CurrentRoom == null) return fallback;

		GameMode gameMode = GetRoomGameMode();
		if (!gameMode.IsTeamMode()) return fallback;

		string[] teamColors = (string[]) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.TEAM_COLORS];
		int teamIndex = (int) player.CustomProperties[PropertyKeys.TEAM_INDEX];

		if (teamIndex < 0 || teamIndex >= teamColors.Length) return fallback;
		string hexColor = teamColors[teamIndex];

		return ColorUtility.TryParseHtmlString("#" + hexColor, out Color color) ? color : fallback;
	}

	public static GameMode GetRoomGameMode()
	{
		return (GameMode) (int) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.GAME_MODE];
	}
}
}