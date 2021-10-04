using System;
using System.Collections;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Simulation.Game;
using Syy1125.OberthEffect.Simulation.UserInterface;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation
{
public class PlayerVehicleSpawner : MonoBehaviour
{
	public float RespawnInterval = 5f;

	public CameraFollow CameraRig;
	public CameraFollow VehicleCamera;
	public PlayerControlConfig ControlConfig;
	public ResourceDisplay ResourceDisplay;
	public BlockHealthBarControl HealthBarControl;
	public Radar Radar;

	public GameObject VehiclePrefab;

	public Text SpawnTextDisplay;

	public GameObject Vehicle { get; private set; }

	private Coroutine _respawn;
	private string _spawnTextTemplate;

	private void SpawnVehicle()
	{
		(Vector3 spawnPosition, Quaternion spawnRotation) = GetSpawnTransform();

		Vehicle = PhotonNetwork.Instantiate(
			VehiclePrefab.name, spawnPosition, spawnRotation,
			0,
			new object[]
			{
				VehicleSelection.SerializedVehicle,
				JsonUtility.ToJson(ColorScheme.FromBlueprint(VehicleSelection.SelectedVehicle))
			}
		);

		CameraRig.Target = Vehicle.transform;
		VehicleCamera.Target = Vehicle.transform;

		ResourceDisplay.ResourceManager = Vehicle.GetComponent<VehicleResourceManager>();
		Vehicle.GetComponent<VehicleThrusterControl>().SetPlayerControlConfig(ControlConfig);
		HealthBarControl.SetTarget(Vehicle.GetComponent<VehicleCore>());
		Radar.OwnVehicle = Vehicle.GetComponent<Rigidbody2D>();

		Vehicle.GetComponent<VehicleCore>().OnVehicleDeath.AddListener(BeginRespawn);
	}

	private Tuple<Vector3, Quaternion> GetSpawnTransform()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return GetFallbackSpawnTransform();
		}

		int teamIndex = PhotonTeamManager.GetPlayerTeamIndex(PhotonNetwork.LocalPlayer);
		Shipyard shipyard = Shipyard.GetShipyardForTeam(teamIndex);

		if (shipyard == null)
		{
			Debug.LogError($"Failed to retrieve shipyard for team index {teamIndex}");
			return GetFallbackSpawnTransform();
		}

		var playersOnTeam = PhotonNetwork.CurrentRoom.Players.Values
			.OrderBy(player => player.ActorNumber)
			.Where(player => PhotonTeamManager.GetPlayerTeamIndex(player) == teamIndex)
			.ToList();

		Transform spawnTransform = shipyard.GetBestSpawnPoint(playersOnTeam.IndexOf(PhotonNetwork.LocalPlayer));
		return new Tuple<Vector3, Quaternion>(spawnTransform.position, spawnTransform.rotation);
	}

	private Tuple<Vector3, Quaternion> GetFallbackSpawnTransform()
	{
		return new Tuple<Vector3, Quaternion>(
			Vector3.right * (10 * PhotonNetwork.LocalPlayer.ActorNumber), Quaternion.identity
		);
	}

	private void Start()
	{
		SpawnVehicle();
		CameraRig.ResetPosition();
	}

	private void OnDisable()
	{
		if (_respawn != null)
		{
			StopCoroutine(_respawn);
			SpawnTextDisplay.text = "";
		}

		if (Vehicle != null)
		{
			Vehicle.GetComponent<VehicleCore>().OnVehicleDeath.RemoveListener(BeginRespawn);
		}
	}

	private void BeginRespawn()
	{
		_spawnTextTemplate = "Respawn in {0}";
		_respawn = StartCoroutine(RespawnSequence());
	}

	private IEnumerator RespawnSequence()
	{
		float timer = RespawnInterval;

		while (timer > CameraRig.InitTime)
		{
			timer -= Time.deltaTime;
			SpawnTextDisplay.text = string.Format(_spawnTextTemplate, Mathf.CeilToInt(timer));
			yield return null;
		}

		SpawnVehicle();
		CameraRig.EnterInitMode();
		SpawnTextDisplay.text = "";
		_respawn = null;
	}
}
}