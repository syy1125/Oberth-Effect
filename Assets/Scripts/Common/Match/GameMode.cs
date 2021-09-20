using System;

namespace Syy1125.OberthEffect.Common.Match
{
public enum GameMode
{
	TestDrive,
	Assault,
	TeamDeathmatch,
}

public static class GameModeExtensions
{
	public static bool EnabledForLobby(this GameMode mode)
	{
		return mode switch
		{
			GameMode.TestDrive => false,
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => false,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static FriendlyFireMode[] GetAllowedFriendlyFireModes(this GameMode mode)
	{
		return mode switch
		{
			GameMode.TestDrive => new[] { FriendlyFireMode.Off },
			GameMode.Assault => new[] { FriendlyFireMode.Off, FriendlyFireMode.Team, FriendlyFireMode.Full },
			GameMode.TeamDeathmatch => new[] { FriendlyFireMode.Off, FriendlyFireMode.Team, FriendlyFireMode.Full },
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static bool IsTeamMode(this GameMode mode)
	{
		return mode switch
		{
			GameMode.TestDrive => false,
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => true,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static bool CanDamageShipyards(this GameMode mode)
	{
		return mode switch
		{
			GameMode.TestDrive => false,
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => false,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}
}
}