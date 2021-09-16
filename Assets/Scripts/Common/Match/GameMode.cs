using System;

namespace Syy1125.OberthEffect.Common.Match
{
public enum GameMode
{
	Assault,
	TeamDeathmatch,
	TestDrive
}

public static class GameModeExtensions
{
	public static bool EnabledForLobby(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => false,
			GameMode.TestDrive => false,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static FriendlyFireMode[] GetAllowedFriendlyFireModes(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Assault => new[] { FriendlyFireMode.Off, FriendlyFireMode.Team, FriendlyFireMode.Full },
			GameMode.TeamDeathmatch => new[] { FriendlyFireMode.Off, FriendlyFireMode.Team, FriendlyFireMode.Full },
			GameMode.TestDrive => new[] { FriendlyFireMode.Off },
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static bool IsTeamMode(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => true,
			GameMode.TestDrive => false,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static bool CanDamageShipyards(this GameMode mode)
	{
		return mode switch
		{
			GameMode.Assault => true,
			GameMode.TeamDeathmatch => false,
			GameMode.TestDrive => false,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}
}
}