using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.MultiplayerLobby
{
public class PlayerPanel : MonoBehaviour
{
	public Text PlayerName;
	public Text PlayerStatus;

	private Player _player;
	private string _vehicleId;

	private void OnEnable()
	{
		RoomScreen.OnVehicleSynchronized += HandleVehicleSynchronized;
	}

	private void OnDisable()
	{
		RoomScreen.OnVehicleSynchronized -= HandleVehicleSynchronized;
	}

	public void AssignPlayer(Player player)
	{
		_player = player;

		PlayerName.text = player.NickName;

		UpdateVehicleDisplay();
	}

	public void UpdateProps(Hashtable props)
	{
		if (props.ContainsKey(PhotonPropertyKeys.VEHICLE_NAME) || props.ContainsKey(PhotonPropertyKeys.VEHICLE_ID))
		{
			UpdateVehicleDisplay();
		}
	}

	private void UpdateVehicleDisplay()
	{
		object vehicleName = _player.CustomProperties[PhotonPropertyKeys.VEHICLE_NAME];

		if (vehicleName == null)
		{
			PlayerStatus.text = "Pondering what to use";
			_vehicleId = null;
		}
		else
		{
			PlayerStatus.text = (string) vehicleName;
			_vehicleId = (string) _player.CustomProperties[PhotonPropertyKeys.VEHICLE_ID];

			if (!RoomScreen.VehicleReady(_vehicleId))
			{
				PlayerStatus.text += " (Synchronizing)";
			}
		}
	}

	private void HandleVehicleSynchronized(string id)
	{
		if (string.Equals(id, _vehicleId))
		{
			UpdateVehicleDisplay();
		}
	}
}
}