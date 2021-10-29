using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class PlayerPanel : MonoBehaviour
{
	public Text PlayerName;
	public Text PlayerStatus;

	public Button KickButton;

	public Image ReadyDisplay;
	public Color ReadyColor;
	public float FadeDuration;

	private Player _player;

	public void AssignPlayer(Player player)
	{
		_player = player;

		UpdateNameDisplay();
		UpdateVehicleDisplay();
		UpdateKickButton();
	}

	private void Start()
	{
		KickButton.onClick.AddListener(KickPlayer);
	}

	public void UpdatePlayerProps(Hashtable props)
	{
		bool vehicleChanged = props.ContainsKey(PropertyKeys.VEHICLE_NAME);
		bool readyChanged = props.ContainsKey(PropertyKeys.PLAYER_READY);
		bool teamChanged = props.ContainsKey(PropertyKeys.TEAM_INDEX);

		if (vehicleChanged)
		{
			UpdateVehicleDisplay();
		}

		if (vehicleChanged || readyChanged || teamChanged)
		{
			UpdateNameDisplay();
		}
	}

	private void UpdateVehicleDisplay()
	{
		object vehicleName = _player.CustomProperties[PropertyKeys.VEHICLE_NAME];

		PlayerStatus.text = vehicleName == null ? "Pondering what to use" : (string) vehicleName;
	}

	public void UpdateNameDisplay()
	{
		bool ready = PhotonHelper.IsPlayerReady(_player);

		StringBuilder playerNameBuilder = new StringBuilder(_player.NickName);
		if (_player.IsMasterClient) playerNameBuilder.Append(" (Host)");
		if (!ready) playerNameBuilder.Append(" (Not Ready)");
		PlayerName.text = playerNameBuilder.ToString();

		PlayerName.color = PhotonTeamManager.GetPlayerTeamColor(_player);

		ReadyDisplay.CrossFadeColor(
			ready ? ReadyColor : Color.white,
			FadeDuration, true, true
		);
	}

	public void UpdateKickButton()
	{
		KickButton.gameObject.SetActive(PhotonNetwork.LocalPlayer.IsMasterClient && !_player.IsLocal);
	}

	private void KickPlayer()
	{
		PhotonNetwork.CloseConnection(_player);
	}
}
}