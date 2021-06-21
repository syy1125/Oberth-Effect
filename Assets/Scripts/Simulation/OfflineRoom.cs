using System;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class OfflineRoom : MonoBehaviour
{
	private void OnEnable()
	{
		PhotonNetwork.OfflineMode = true;
		PhotonNetwork.CreateRoom("");
	}

	private void OnDisable()
	{
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.OfflineMode = false;
	}
}
}