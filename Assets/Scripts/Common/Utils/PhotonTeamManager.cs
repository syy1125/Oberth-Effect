using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Match;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public class PhotonTeamManager
{
	public static int[] GetTeams()
	{
		GameMode gameMode = PhotonHelper.GetRoomGameMode();

		if (gameMode.IsTeamMode())
		{
			int teamCount = ((string[]) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.TEAM_COLORS]).Length;

			int[] teams = new int[teamCount];
			for (int i = 0; i < teamCount; i++)
			{
				teams[i] = i;
			}

			return teams;
		}
		else
		{
			return PhotonNetwork.CurrentRoom.Players.Values
				.Select(player => player.ActorNumber)
				.ToArray();
		}
	}

	public static int GetPlayerTeamIndex(Player player)
	{
		GameMode gameMode = PhotonHelper.GetRoomGameMode();

		if (gameMode.IsTeamMode())
		{
			return (int) player.CustomProperties[PropertyKeys.TEAM_INDEX];
		}
		else
		{
			return player.ActorNumber;
		}
	}

	public static Color GetTeamColor(int teamIndex)
	{
		return GetTeamColor(teamIndex, Color.white);
	}

	public static Color GetTeamColor(int teamIndex, Color fallback)
	{
		if (PhotonNetwork.CurrentRoom == null) return fallback;

		GameMode gameMode = PhotonHelper.GetRoomGameMode();
		if (!gameMode.IsTeamMode()) return fallback;

		string[] teamColors = (string[]) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.TEAM_COLORS];

		if (teamIndex < 0 || teamIndex >= teamColors.Length) return fallback;
		string hexColor = teamColors[teamIndex];

		return ColorUtility.TryParseHtmlString("#" + hexColor, out Color color) ? color : fallback;
	}

	public static Color GetPlayerTeamColor(Player player)
	{
		return GetPlayerTeamColor(player, Color.white);
	}

	public static Color GetPlayerTeamColor(Player player, Color fallback)
	{
		return GetTeamColor(GetPlayerTeamIndex(player), fallback);
	}
}
}