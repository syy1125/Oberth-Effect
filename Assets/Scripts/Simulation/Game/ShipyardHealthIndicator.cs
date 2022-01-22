using Syy1125.OberthEffect.Common.Match;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.Game
{
[RequireComponent(typeof(Image))]
public class ShipyardHealthIndicator : MonoBehaviour
{
	public Shipyard Shipyard;
	private Image _image;

	private void Awake()
	{
		_image = GetComponent<Image>();
	}

	private void Start()
	{
		_image.color = PhotonTeamHelper.GetTeamColors(Shipyard.TeamIndex).PrimaryColor;

		var gameMode = PhotonHelper.GetRoomGameMode();
		if (!gameMode.CanDamageShipyards())
		{
			gameObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		_image.fillAmount = Shipyard.Health / Shipyard.MaxHealth;
	}
}
}