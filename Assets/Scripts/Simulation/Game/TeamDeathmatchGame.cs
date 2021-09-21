using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Utils;

namespace Syy1125.OberthEffect.Simulation.Game
{
public class TeamDeathmatchGame : AbstractGameManager
{
	private void Awake()
	{
		if (PhotonHelper.GetRoomGameMode() == GameMode.TeamDeathmatch)
		{
			GameManager = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private void OnDestroy()
	{
		if (GameManager == this)
		{
			GameManager = null;
		}
	}

	public override void OnShipyardDestroyed(int teamIndex)
	{
		// No-op. TDM shipyard should not be destroyed in the first place.
	}
}
}