﻿using Photon.Pun;
using Syy1125.OberthEffect.Simulation.UserInterface;
using Syy1125.OberthEffect.Simulation.Vehicle;
using Syy1125.OberthEffect.Utils;

namespace Syy1125.OberthEffect.Simulation.Game
{
public abstract class AbstractGameManager : MonoBehaviourPunCallbacks
{
	public PlayerVehicleSpawner VehicleSpawner;
	public VictoryDefeatScreen EndScreen;

	public static AbstractGameManager GameManager { get; protected set; }

	public abstract void OnShipyardDestroyed(int teamIndex);

	[PunRPC]
	public void SetDefeated(int teamIndex)
	{
		if (PhotonTeamManager.GetPlayerTeamIndex(PhotonNetwork.LocalPlayer) == teamIndex)
		{
			EndScreen.ShowDefeat();
			DisableControls();
		}
	}

	[PunRPC]
	public void SetVictory(int teamIndex)
	{
		if (PhotonTeamManager.GetPlayerTeamIndex(PhotonNetwork.LocalPlayer) == teamIndex)
		{
			EndScreen.ShowVictory();
			DisableControls();
		}
	}

	protected void DisableControls()
	{
		VehicleSpawner.enabled = false;
		VehicleSpawner.Vehicle.GetComponent<VehicleCore>().Die();
	}
}
}