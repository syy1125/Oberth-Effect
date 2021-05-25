using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.MultiplayerLobby;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Multiplayer
{
public class PlayerVehicleSpawner : MonoBehaviour
{
	public CameraFollow CameraRig;
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
		CameraRig.Target = vehicle.transform;
	}
}
}