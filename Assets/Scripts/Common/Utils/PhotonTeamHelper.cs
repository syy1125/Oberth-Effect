using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Match;
using ColorScheme = Syy1125.OberthEffect.Common.Colors.ColorScheme;
using UnityEngine;

namespace Syy1125.OberthEffect.Common.Utils
{
public static class PhotonTeamHelper
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

	public static bool IsValidTeam(int teamIndex)
	{
		return teamIndex >= 0;
	}

	public static void SetPlayerTeam(Player player, int teamIndex)
	{
		player.SetCustomProperties(
			new Hashtable
			{
				{ PropertyKeys.TEAM_INDEX, teamIndex },
				{ PropertyKeys.PLAYER_READY, false }
			}
		);
	}

	public static ColorScheme GetTeamColors(int teamIndex)
	{
		return GetTeamColors(teamIndex, Color.white);
	}

	public static ColorScheme GetTeamColors(int teamIndex, Color fallbackColor)
	{
		ColorScheme fallback = new ColorScheme
			{ PrimaryColor = fallbackColor, SecondaryColor = fallbackColor, TertiaryColor = fallbackColor };

		if (PhotonNetwork.CurrentRoom == null) return fallback;

		GameMode gameMode = PhotonHelper.GetRoomGameMode();
		if (!gameMode.IsTeamMode()) return fallback;

		string[] teamColors = (string[]) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.TEAM_COLORS];

		if (teamIndex < 0 || teamIndex >= teamColors.Length) return fallback;
		string[] colors = teamColors[teamIndex].Split(';');

		return ColorUtility.TryParseHtmlString("#" + colors[0], out Color primary)
		       && ColorUtility.TryParseHtmlString("#" + colors[1], out Color secondary)
		       && ColorUtility.TryParseHtmlString("#" + colors[2], out Color tertiary)
			? new ColorScheme { PrimaryColor = primary, SecondaryColor = secondary, TertiaryColor = tertiary }
			: fallback;
	}

	public static ColorScheme GetPlayerTeamColors(Player player)
	{
		return GetPlayerTeamColors(player, Color.white);
	}

	public static ColorScheme GetPlayerTeamColors(Player player, Color fallback)
	{
		return GetTeamColors(GetPlayerTeamIndex(player), fallback);
	}
}
}