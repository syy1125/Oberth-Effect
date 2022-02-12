using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Match;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation.Utils
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

	public static int GetPlayerTeamIndex(int playerActorNumber)
	{
		return GetPlayerTeamIndex(PhotonNetwork.CurrentRoom.Players[playerActorNumber]);
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

	public static ColorScheme GetTeamColors(int teamIndex, Color fallback)
	{
		return GetTeamColors(teamIndex, new ColorScheme(fallback, fallback, fallback));
	}

	public static ColorScheme GetTeamColors(int teamIndex, ColorScheme fallback)
	{
		if (PhotonNetwork.CurrentRoom == null) return fallback;

		GameMode gameMode = PhotonHelper.GetRoomGameMode();
		if (!gameMode.IsTeamMode()) return fallback;

		string[] teamColors = (string[]) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.TEAM_COLORS];

		if (teamIndex < 0 || teamIndex >= teamColors.Length) return fallback;
		return ColorScheme.TryParseColorSet(teamColors[teamIndex], out ColorScheme colorScheme)
			? colorScheme
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

	public static ColorScheme GetPlayerTeamColors(Player player, ColorScheme fallback)
	{
		return GetTeamColors(GetPlayerTeamIndex(player), fallback);
	}
}
}