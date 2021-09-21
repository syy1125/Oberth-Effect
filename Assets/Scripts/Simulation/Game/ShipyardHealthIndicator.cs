using Syy1125.OberthEffect.Utils;
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

	private void LateUpdate()
	{
		_image.fillAmount = Shipyard.Health / Shipyard.MaxHealth;
	}
}
}