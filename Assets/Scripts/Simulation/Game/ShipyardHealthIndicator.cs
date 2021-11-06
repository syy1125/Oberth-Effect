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
		_image.color = PhotonTeamManager.GetTeamColor(Shipyard.TeamIndex);
	}

	private void Start()
	{
		var gameMode = PhotonHelper.GetRoomGameMode();
		if (!gameMode.CanDamageShipyards())
		{
			gameObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		_image.fillAmount = Shipyard.Health / Shipyard.BaseMaxHealth;
	}
}
}