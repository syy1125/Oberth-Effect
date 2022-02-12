using System;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Match;
using Syy1125.OberthEffect.Foundation.UserInterface;
using Syy1125.OberthEffect.Foundation.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class RoomControls : MonoBehaviourPunCallbacks
{
	[Header("Room Name")]
	public Text RoomName;
	public InputField RoomNameInput;
	public Toggle UseTeamColors;

	[Header("Controls")]
	public Button SelectVehicleButton;
	public GameObject VehicleSelectionScreen;
	public VehicleList VehicleList;
	public Button LoadVehicleButton;
	public Button ReadyButton;
	public Tooltip ReadyTooltip;

	[Space]
	public SwitchSelect GameModeSelect;
	public SwitchSelect CostLimitSelect;
	public InputField CostLimitInput;
	public InputField ShipyardHealthInput;
	public Button StartGameButton;
	public Tooltip StartGameTooltip;

	[Header("References")]
	public GameObject LobbyScreen;

	[Space]
	public SceneReference[] Maps;

	private GameMode[] _gameModes;
	// The vehicle that the player is considering to load. Transient state.
	private string _selectedVehicleName;

	private void Awake()
	{
		_gameModes = Enum.GetValues(typeof(GameMode))
			.Cast<GameMode>()
			.Where(gameMode => gameMode.EnabledForLobby())
			.ToArray();
	}

	public override void OnEnable()
	{
		base.OnEnable();

		RoomName.text = RoomNameInput.text =
			(string) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.ROOM_NAME];
		RoomNameInput.onEndEdit.AddListener(SetRoomName);
		UseTeamColors.isOn = (bool) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.USE_TEAM_COLORS];
		UseTeamColors.onValueChanged.AddListener(SetUseTeamColor);

		_selectedVehicleName = null;
		VehicleSelection.SerializedVehicle = null;
		VehicleList.OnSelectVehicle.AddListener(SelectVehicle);
		LoadVehicleButton.interactable = false;
		LoadVehicleButton.onClick.AddListener(LoadVehicleSelection);

		GameModeSelect.SetOptions(_gameModes.Select(gameMode => Enum.GetName(typeof(GameMode), gameMode)).ToArray());
		GameModeSelect.OnValueChanged.AddListener(SetGameMode);
		CostLimitSelect.OnValueChanged.AddListener(SetCostLimitPresetOption);
		CostLimitInput.onEndEdit.AddListener(SetCostLimitText);
		ShipyardHealthInput.onEndEdit.AddListener(SetShipyardHealthMultiplier);

		SelectVehicleButton.interactable = true;
		ReadyButton.interactable = false;
		ReadyTooltip.enabled = true;
		SelectVehicleButton.onClick.AddListener(OpenVehicleSelection);
		ReadyButton.onClick.AddListener(ToggleReady);
		StartGameButton.onClick.AddListener(StartGame);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UseMasterControls();
			UpdateMasterControls();
		}
		else
		{
			UseClientControls();
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();

		RoomNameInput.onEndEdit.RemoveListener(SetRoomName);
		UseTeamColors.onValueChanged.RemoveListener(SetUseTeamColor);

		VehicleList.OnSelectVehicle.RemoveListener(SelectVehicle);
		LoadVehicleButton.onClick.RemoveListener(LoadVehicleSelection);

		GameModeSelect.OnValueChanged.RemoveListener(SetGameMode);
		CostLimitSelect.OnValueChanged.RemoveListener(SetCostLimitPresetOption);
		CostLimitInput.onEndEdit.RemoveListener(SetCostLimitText);
		ShipyardHealthInput.onEndEdit.RemoveListener(SetShipyardHealthMultiplier);

		SelectVehicleButton.onClick.RemoveListener(OpenVehicleSelection);
		ReadyButton.onClick.RemoveListener(ToggleReady);
		StartGameButton.onClick.RemoveListener(StartGame);
	}

	#region Photon Callbacks

	public override void OnRoomPropertiesUpdate(Hashtable nextProps)
	{
		if (nextProps.ContainsKey(PropertyKeys.ROOM_NAME))
		{
			RoomName.text = RoomNameInput.text = (string) nextProps[PropertyKeys.ROOM_NAME];
		}

		if (ContainsAnyKey(nextProps, PropertyKeys.GAME_MODE, PropertyKeys.COST_LIMIT_OPTION, PropertyKeys.COST_LIMIT))
		{
			if (!PhotonNetwork.LocalPlayer.IsMasterClient)
			{
				UpdateClientControls();
			}
		}

		if (nextProps.ContainsKey(PropertyKeys.COST_LIMIT))
		{
			UpdateVehicleListCostLimit();
		}
	}

	private static bool ContainsAnyKey(Hashtable table, params string[] keys)
	{
		return keys.Any(table.ContainsKey);
	}

	public override void OnPlayerEnteredRoom(Player player)
	{
		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
	}

	public override void OnPlayerLeftRoom(Player player)
	{
		// If the master player left the room, one of the other player would take over.
		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UseMasterControls();
			UpdateMasterControls();
		}
		else
		{
			UseClientControls();
		}
	}

	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable props)
	{
		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
		else
		{
			UpdateClientControls();
		}
	}

	#endregion

	#region Master Client Controls

	private void SetRoomName(string roomName)
	{
		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PropertyKeys.ROOM_NAME, roomName } }
		);
	}

	private void SetUseTeamColor(bool useTeamColor)
	{
		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PropertyKeys.USE_TEAM_COLORS, useTeamColor } }
		);
	}

	private static void UnreadyPlayers()
	{
		foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
		{
			if (!player.IsMasterClient)
			{
				player.SetCustomProperties(
					new Hashtable { { PropertyKeys.PLAYER_READY, false } }
				);
			}
		}
	}

	private void SetGameMode(int i)
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient) return;

		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable { { PropertyKeys.GAME_MODE, _gameModes[i] } }
		);
		UnreadyPlayers();
	}

	private void SetCostLimitPresetOption(int optionIndex)
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient) return;

		int teamPlayerCount = Mathf.Max(PhotonNetwork.CurrentRoom.PlayerCount / 2, 1);

		int costLimit = optionIndex switch
		{
			0 => 1000,
			1 => 3000,
			2 => 10000,
			3 => 1000,
			_ => throw new ArgumentOutOfRangeException(nameof(optionIndex), optionIndex, null)
		};

		float shipyardHealthMultiplier = optionIndex switch
		{
			0 => 1f * teamPlayerCount,
			1 => 3f * teamPlayerCount,
			2 => 10f * teamPlayerCount,
			3 => 1f * teamPlayerCount,
			_ => throw new ArgumentOutOfRangeException(nameof(optionIndex), optionIndex, null)
		};

		PhotonNetwork.CurrentRoom.SetCustomProperties(
			new Hashtable
			{
				{ PropertyKeys.COST_LIMIT_OPTION, optionIndex },
				{ PropertyKeys.COST_LIMIT, costLimit },
				{ PropertyKeys.SHIPYARD_HEALTH_MULTIPLIER, shipyardHealthMultiplier }
			}
		);

		CostLimitInput.text = costLimit.ToString();
		CostLimitInput.interactable = optionIndex == 3;
		ShipyardHealthInput.text = shipyardHealthMultiplier.ToString("0.##");

		UnreadyPlayers();
	}

	private void SetCostLimitText(string text)
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient) return;

		int currentLimit = PhotonHelper.GetRoomCostLimit();

		if (int.TryParse(text, out int value) && value > 0)
		{
			if (value != currentLimit)
			{
				PhotonNetwork.CurrentRoom.SetCustomProperties(
					new Hashtable { { PropertyKeys.COST_LIMIT, value } }
				);
				UnreadyPlayers();
			}
		}
		else
		{
			PhotonNetwork.CurrentRoom.SetCustomProperties(
				new Hashtable { { PropertyKeys.COST_LIMIT, currentLimit } }
			);
			CostLimitInput.text = currentLimit.ToString();
		}
	}

	private void SetShipyardHealthMultiplier(string multiplier)
	{
		if (!PhotonNetwork.LocalPlayer.IsMasterClient) return;

		if (float.TryParse(multiplier, out float value) && value > Mathf.Epsilon)
		{
			PhotonNetwork.CurrentRoom.SetCustomProperties(
				new Hashtable { { PropertyKeys.SHIPYARD_HEALTH_MULTIPLIER, value } }
			);
		}
		else
		{
			float currentValue = PhotonHelper.GetShipyardHealthMultiplier();
			PhotonNetwork.CurrentRoom.SetCustomProperties(
				new Hashtable { { PropertyKeys.SHIPYARD_HEALTH_MULTIPLIER, currentValue } }
			);
			ShipyardHealthInput.text = currentValue.ToString("0.##");
		}
	}

	#endregion

	#region Update Controls

	private void UseMasterControls()
	{
		RoomName.gameObject.SetActive(false);
		RoomNameInput.gameObject.SetActive(true);
		UseTeamColors.interactable = true;

		GameModeSelect.Interactable = true;
		CostLimitSelect.Interactable = true;
		CostLimitInput.interactable = Equals(
			PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.COST_LIMIT_OPTION], 3
		);
		ShipyardHealthInput.interactable = true;

		ReadyButton.gameObject.SetActive(false);
		StartGameButton.gameObject.SetActive(true);
	}

	private void UseClientControls()
	{
		RoomName.gameObject.SetActive(true);
		RoomNameInput.gameObject.SetActive(false);
		UseTeamColors.interactable = false;

		GameModeSelect.Interactable = false;
		CostLimitSelect.Interactable = false;
		CostLimitInput.interactable = false;
		ShipyardHealthInput.interactable = true;

		ReadyButton.gameObject.SetActive(true);
		StartGameButton.gameObject.SetActive(false);
	}

	private void UpdateMasterControls()
	{
		SelectVehicleButton.interactable = true;

		GameModeSelect.Value = Array.IndexOf(
			_gameModes, (GameMode) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.GAME_MODE]
		);
		CostLimitSelect.Value = (int) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.COST_LIMIT_OPTION];
		CostLimitInput.text = PhotonHelper.GetRoomCostLimit().ToString();
		ShipyardHealthInput.text = PhotonHelper.GetShipyardHealthMultiplier().ToString("0.##");

		bool allReady = PhotonNetwork.CurrentRoom.Players.Values.All(PhotonHelper.IsPlayerReady);
		StartGameButton.interactable = allReady;
		StartGameTooltip.enabled = !allReady;
	}

	private void UpdateClientControls()
	{
		bool ready = PhotonHelper.IsPlayerReady(PhotonNetwork.LocalPlayer);
		SelectVehicleButton.interactable = !ready;

		GameModeSelect.Value = Array.IndexOf(
			_gameModes, (GameMode) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.GAME_MODE]
		);
		CostLimitSelect.Value = (int) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.COST_LIMIT_OPTION];
		CostLimitInput.text = PhotonHelper.GetRoomCostLimit().ToString();
		ShipyardHealthInput.text = PhotonHelper.GetShipyardHealthMultiplier().ToString("0.##");

		ReadyButton.interactable = VehicleSelection.SerializedVehicle != null;
		ReadyButton.GetComponentInChildren<Text>().text = ready ? "Unready" : "Ready";
		ReadyTooltip.enabled = VehicleSelection.SerializedVehicle == null;
	}

	#endregion

	private void UpdateVehicleListCostLimit()
	{
		var costLimit = (int) PhotonNetwork.CurrentRoom.CustomProperties[PropertyKeys.COST_LIMIT];
		VehicleList.SetCostLimit(costLimit);

		// If the player has loaded a selected vehicle but that vehicle is now over limit, de-select it.
		if (VehicleSelection.SelectedVehicle != null && VehicleSelection.SelectedVehicle.CachedCost > costLimit)
		{
			VehicleSelection.SerializedVehicle = null;
			PhotonNetwork.LocalPlayer.SetCustomProperties(
				new Hashtable { { PropertyKeys.VEHICLE_NAME, null }, { PropertyKeys.PLAYER_READY, false } }
			);
		}

		// If the vehicle list is open and the player has selected a vehicle but not confirmed it, update whether the load vehicle button can be clicked.
		if (VehicleSelectionScreen.activeSelf && _selectedVehicleName != null)
		{
			string serializedVehicle = File.ReadAllText(VehicleList.ToVehiclePath(_selectedVehicleName));
			VehicleBlueprint blueprint = JsonUtility.FromJson<VehicleBlueprint>(serializedVehicle);
			LoadVehicleButton.interactable = blueprint.CachedCost <= PhotonHelper.GetRoomCostLimit();
		}
	}

	private void OpenVehicleSelection()
	{
		SelectVehicle(null);
		UpdateVehicleListCostLimit();
		VehicleSelectionScreen.SetActive(true);
	}

	private void SelectVehicle(string vehicleName)
	{
		_selectedVehicleName = vehicleName;

		if (vehicleName != null)
		{
			string serializedVehicle = File.ReadAllText(VehicleList.ToVehiclePath(_selectedVehicleName));
			VehicleBlueprint blueprint = JsonUtility.FromJson<VehicleBlueprint>(serializedVehicle);
			LoadVehicleButton.interactable = blueprint.CachedCost <= PhotonHelper.GetRoomCostLimit();
		}
		else
		{
			LoadVehicleButton.interactable = false;
		}
	}

	private void LoadVehicleSelection()
	{
		string serializedVehicle = File.ReadAllText(VehicleList.ToVehiclePath(_selectedVehicleName));
		VehicleSelection.SerializedVehicle = serializedVehicle;

		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable
			{
				{ PropertyKeys.VEHICLE_NAME, _selectedVehicleName },
				{ PropertyKeys.VEHICLE_COST, VehicleSelection.SelectedVehicle.CachedCost }
			}
		);

		VehicleSelectionScreen.SetActive(false);

		if (PhotonNetwork.LocalPlayer.IsMasterClient)
		{
			UpdateMasterControls();
		}
		else
		{
			UpdateClientControls();
		}
	}

	private void ToggleReady()
	{
		bool ready = PhotonHelper.IsPlayerReady(PhotonNetwork.LocalPlayer);
		PhotonNetwork.LocalPlayer.SetCustomProperties(
			new Hashtable { { PropertyKeys.PLAYER_READY, !ready } },
			new Hashtable { { PropertyKeys.PLAYER_READY, ready } }
		);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
	}

	public override void OnLeftRoom()
	{
		VehicleSelection.SerializedVehicle = null;

		gameObject.SetActive(false);
		LobbyScreen.SetActive(true);
	}

	private void StartGame()
	{
		PhotonNetwork.AutomaticallySyncScene = true;
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.LoadLevel(Maps[0].ScenePath);
	}
}
}