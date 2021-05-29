using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Lobby.MultiplayerLobby;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.Multiplayer
{
public class PlayerVehicleSpawner : MonoBehaviour
{
	public CameraFollow CameraRig;
	public CameraFollow VehicleCamera;
	public Text InertiaDampenerStatusIndicator;
	public GameObject VehiclePrefab;

	private void Start()
	{
		GameObject vehicle = PhotonNetwork.Instantiate(
			VehiclePrefab.name,
			Vector3.right * (10 * PhotonNetwork.LocalPlayer.ActorNumber), Quaternion.identity,
			0,
			new object[]
			{
				RoomScreen.SerializedSelectedVehicle,
				JsonUtility.ToJson(ColorScheme.FromBlueprint(RoomScreen.SelectedVehicle))
			}
		);
		vehicle.GetComponent<VehicleThrusterControl>().InertiaDampenerStatusIndicator = InertiaDampenerStatusIndicator;

		CameraRig.Target = vehicle.transform;
		VehicleCamera.Target = vehicle.transform;
	}
}
}