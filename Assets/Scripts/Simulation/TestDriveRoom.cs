using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Match;
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