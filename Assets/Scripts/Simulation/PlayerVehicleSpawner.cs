using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Simulation.UserInterface;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class PlayerVehicleSpawner : MonoBehaviour
{
	public CameraFollow CameraRig;
	public CameraFollow VehicleCamera;
	public VehicleControlDisplay ControlDisplay;
	public ResourceDisplay ResourceDisplay;

	public GameObject VehiclePrefab;

	private void Start()
	{
		GameObject vehicle = PhotonNetwork.Instantiate(
			VehiclePrefab.name,
			Vector3.right * (10 * PhotonNetwork.LocalPlayer.ActorNumber), Quaternion.identity,
			0,
			new object[]
			{
				VehicleSelection.SerializedVehicle,
				JsonUtility.ToJson(ColorScheme.FromBlueprint(VehicleSelection.SelectedVehicle))
			}
		);

		CameraRig.Target = vehicle.transform;
		VehicleCamera.Target = vehicle.transform;
		ControlDisplay.ThrusterControl = vehicle.GetComponent<VehicleThrusterControl>();
		ResourceDisplay.ResourceManager = vehicle.GetComponent<VehicleResourceManager>();
	}
}
}