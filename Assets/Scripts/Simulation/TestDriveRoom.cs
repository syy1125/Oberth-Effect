using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class TestDriveRoom : MonoBehaviour
{
	private void OnEnable()
	{
		PhotonNetwork.OfflineMode = true;
		PhotonNetwork.CreateRoom(
			"", new RoomOptions
			{
				CustomRoomProperties = new Hashtable { { PropertyKeys.GAME_MODE, GameMode.TestDrive } }
			}
		);
	}

	private void OnDisable()
	{
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.OfflineMode = false;
	}
}
}