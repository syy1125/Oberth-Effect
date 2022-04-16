using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Lobby.Multiplayer
{
public class PlayerPanel : MonoBehaviourPunCallbacks
{
	public TMP_Text PlayerName;
	public Text PlayerStatus;
	public Text ReadyStatus;

	public Button KickButton;

	public Image ReadyDisplay;
	public Color ReadyColor;
	public float FadeDuration;

	public Player Player { get; private set; }

	public void AssignPlayer(Player player)
	{
		Player = player;

		UpdateNameDisplay();
		UpdateVehicleDisplay();
		UpdateKickButton();
	}

	private void Start()
	{
		KickButton.onClick.AddListener(KickPlayer);
	}

	public override void OnPlayerPropertiesUpdate(Player player, Hashtable props)
	{
		if (player.Equals(Player))
		{
			bool vehicleChanged = props.ContainsKey(PropertyKeys.VEHICLE_NAME);
			bool readyChanged = props.ContainsKey(PropertyKeys.PLAYER_READY);

			if (vehicleChanged)
			{
				UpdateVehicleDisplay();
			}

			if (vehicleChanged || readyChanged)
			{
				UpdateNameDisplay();
			}
		}
	}

	private void UpdateVehicleDisplay()
	{
		string vehicleName = (string) Player.CustomProperties[PropertyKeys.VEHICLE_NAME];
		int vehicleCost = (int) Player.CustomProperties[PropertyKeys.VEHICLE_COST];

		PlayerStatus.text = vehicleName == null
			? "Pondering what to use"
			: $"Selected vehicle: \"{vehicleName}\" (<color=\"lime\">{vehicleCost}</color>)";
	}

	public void UpdateNameDisplay()
	{
		bool ready = PhotonHelper.IsPlayerReady(Player);

		PlayerName.text = Player.NickName;

		List<string> playerTags = new List<string>();
		if (Player.IsMasterClient) playerTags.Add("Host");
		if (!ready) playerTags.Add("Not Ready");
		ReadyStatus.text = string.Join(", ", playerTags);

		ReadyDisplay.CrossFadeColor(
			ready ? ReadyColor : Color.white,
			FadeDuration, true, true
		);
	}

	public void UpdateKickButton()
	{
		KickButton.gameObject.SetActive(PhotonNetwork.LocalPlayer.IsMasterClient && !Player.IsLocal);
	}

	private void KickPlayer()
	{
		Debug.Log($"Kicking player {Player}");
		PhotonNetwork.CloseConnection(Player);
	}
}
}