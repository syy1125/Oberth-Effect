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
	public VehicleInfoDisplay InfoDisplay;
	public ResourceDisplay ResourceDisplay;
	public BlockHealthBarControl HealthBarControl;

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
		InfoDisplay.ThrusterControl = vehicle.GetComponent<VehicleThrusterControl>();
		ResourceDisplay.ResourceManager = vehicle.GetComponent<VehicleResourceManager>();
		HealthBarControl.SetTarget(vehicle.GetComponent<VehicleCore>());
	}
}
}