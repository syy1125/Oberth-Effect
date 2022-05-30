using System;
using System.Collections;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Match;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.Simulation.Game;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation
{
[Serializable]
public class VehicleChangeEvent : UnityEvent<GameObject>
{}

public class PlayerVehicleSpawner : MonoBehaviour
{
	public static PlayerVehicleSpawner Instance { get; private set; }

	public float RespawnInterval = 5f;

	[Header("References")]
	public CameraFollow CameraRig;

	public GameObject VehiclePrefab;

	public GameObject SpawnPanel;
	public Text SpawnTextDisplay;

	[Header("Self Destruct")]
	public InputActionReference SelfDestructAction;
	public float SelfDestructHoldTime = 3f;

	public GameObject SelfDestructPanel;
	public Image SelfDestructProgress;
	public Text SelfDestructDisplay;

	public GameObject Vehicle { get; private set; }
	public VehicleChangeEvent OnVehicleChanged = new VehicleChangeEvent();

	private Coroutine _respawn;
	private string _spawnTextTemplate;
	private float? _selfDestructStart;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Debug.LogError("Multiple PlayerVehicleSpawner were instantiated");
			Destroy(this);
		}
	}

	private void OnEnable()
	{
		SelfDestructAction.action.started += HandleSelfDestructStart;
		SelfDestructAction.action.canceled += HandleSelfDestructCancel;
	}

	private void Start()
	{
		SpawnVehicle();
		CameraRig.ResetPosition();
		SpawnPanel.SetActive(false);
		SelfDestructPanel.SetActive(false);
	}

	private void Update()
	{
		if (_selfDestructStart != null)
		{
			float progress = Time.time - _selfDestructStart.Value;
			if (progress < SelfDestructHoldTime)
			{
				SelfDestructPanel.SetActive(true);
				SelfDestructProgress.fillAmount = progress / SelfDestructHoldTime;
				SelfDestructDisplay.text = $"Self destruct in {Mathf.CeilToInt(SelfDestructHoldTime - progress)}";
			}
			else
			{
				_selfDestructStart = null;
				SelfDestructPanel.SetActive(false);

				Vehicle.GetComponent<PhotonView>().RPC(nameof(VehicleCore.DisableVehicle), RpcTarget.AllBuffered);
			}
		}
		else
		{
			SelfDestructPanel.SetActive(false);
		}
	}

	private void OnDisable()
	{
		SelfDestructAction.action.started -= HandleSelfDestructStart;
		SelfDestructAction.action.canceled -= HandleSelfDestructCancel;

		if (_respawn != null)
		{
			StopCoroutine(_respawn);
			SpawnPanel.SetActive(false);
		}

		if (Vehicle != null)
		{
			Vehicle.GetComponent<VehicleCore>().OnVehicleDeath.RemoveListener(BeginRespawn);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void SpawnVehicle()
	{
		(Vector3 spawnPosition, Quaternion spawnRotation, Vector2 velocity) = GetSpawnConditions();

		bool useTeamColors = PhotonHelper.GetRoomGameMode().IsTeamMode()
		                     && (bool) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.USE_TEAM_COLORS];

		ColorScheme colorScheme = ColorScheme.FromBlueprint(VehicleSelection.SelectedVehicle);
		if (useTeamColors) colorScheme = PhotonTeamHelper.GetPlayerTeamColors(PhotonNetwork.LocalPlayer, colorScheme);

		Vehicle = PhotonNetwork.Instantiate(
			VehiclePrefab.name, spawnPosition, spawnRotation,
			0,
			new object[]
			{
				CompressionUtils.Compress(VehicleSelection.SerializedVehicle),
				JsonUtility.ToJson(colorScheme)
			}
		);

		Vehicle.GetComponent<VehicleCore>().IsMainVehicle = true;

		Vehicle.GetComponent<Rigidbody2D>().velocity = velocity;
		Vehicle.GetComponent<VehicleCore>().OnVehicleDeath.AddListener(BeginRespawn);
		ReferenceFrameProvider.MainReferenceFrame = Vehicle.GetComponent<ReferenceFrameProvider>();

		OnVehicleChanged.Invoke(Vehicle);
	}

	private Tuple<Vector3, Quaternion, Vector2> GetSpawnConditions()
	{
		if (PhotonNetwork.CurrentRoom == null)
		{
			return GetFallbackSpawn();
		}

		int teamIndex = PhotonTeamHelper.GetPlayerTeamIndex(PhotonNetwork.LocalPlayer);
		Shipyard shipyard = Shipyard.GetShipyardForTeam(teamIndex);

		if (shipyard == null)
		{
			Debug.LogError($"Failed to retrieve shipyard for team index {teamIndex}");
			return GetFallbackSpawn();
		}

		var playersOnTeam = PhotonNetwork.CurrentRoom.Players.Values
			.OrderBy(player => player.ActorNumber)
			.Where(player => PhotonTeamHelper.GetPlayerTeamIndex(player) == teamIndex)
			.ToList();

		Transform spawnTransform = shipyard.GetBestSpawnPoint(playersOnTeam.IndexOf(PhotonNetwork.LocalPlayer));
		return Tuple.Create(spawnTransform.position, spawnTransform.rotation, shipyard.GetVelocity());
	}

	private Tuple<Vector3, Quaternion, Vector2> GetFallbackSpawn()
	{
		return Tuple.Create(
			Vector3.right * (10 * PhotonNetwork.LocalPlayer.ActorNumber), Quaternion.identity, Vector2.zero
		);
	}

	private void BeginRespawn()
	{
		Vehicle = null;

		_selfDestructStart = null;
		_spawnTextTemplate = "Respawn in {0}";
		_respawn = StartCoroutine(RespawnSequence());
	}

	private IEnumerator RespawnSequence()
	{
		SpawnPanel.SetActive(true);
		float timer = RespawnInterval;

		while (timer > CameraRig.InitTime)
		{
			timer -= Time.deltaTime;
			SpawnTextDisplay.text = string.Format(_spawnTextTemplate, Mathf.CeilToInt(timer));
			yield return null;
		}

		SpawnVehicle();
		CameraRig.EnterInitMode();

		yield return new WaitForSeconds(CameraRig.InitTime);

		SpawnPanel.SetActive(false);
		_respawn = null;
	}

	private void HandleSelfDestructStart(InputAction.CallbackContext context)
	{
		if (Vehicle != null && _respawn == null)
		{
			_selfDestructStart = Time.time;
		}
	}

	private void HandleSelfDestructCancel(InputAction.CallbackContext context)
	{
		_selfDestructStart = null;
	}
}
}