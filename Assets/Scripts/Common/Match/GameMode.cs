using System;

namespace Syy1125.OberthEffect.Common.Match
{
public enum GameMode
{
	Assault,
	TeamDeathmatch,
}

public static class GameModeExtensions
{
	public static FriendlyFireMode[] GetAllowedFriendlyFireModes(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Assault => new[] { FriendlyFireMode.Off, FriendlyFireMode.Team, FriendlyFireMode.Full },
			GameMode.TeamDeathmatch => new[] { FriendlyFireMode.Off, FriendlyFireMode.Team, FriendlyFireMode.Full },
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static bool IsTeamMode(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => true,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}
}
}