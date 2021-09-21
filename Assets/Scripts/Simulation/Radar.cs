using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Simulation.Game;
using Syy1125.OberthEffect.Simulation.Vehicle;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation
{
public class Radar : MonoBehaviour
{
	public Rigidbody2D OwnVehicle;
	public GameObject RadarPingPrefab;
	public Sprite VehiclePingSprite;
	public Sprite ShipyardPingSprite;
	public float Scale = 1e-3f;

	private List<GameObject> _pings;

	private void Awake()
	{
		_pings = new List<GameObject>();
	}

	private void LateUpdate()
	{
		int i = 0;
		Vector2 centerOfMass = OwnVehicle.worldCenterOfMass;

		foreach (VehicleCore vehicle in VehicleCore.ActiveVehicles)
		{
			if (vehicle == null) continue;

			Vector2 relativePosition = vehicle.GetComponent<Rigidbody2D>().worldCenterOfMass - centerOfMass;

			GameObject ping = GetOrCreatePing(i);

			ping.transform.localPosition = relativePosition * Scale;
			ping.transform.rotation = vehicle.transform.rotation;
			ping.GetComponent<Image>().sprite = VehiclePingSprite;
			ping.GetComponent<Image>().color =
				PhotonTeamManager.GetPlayerTeamColor(vehicle.GetComponent<PhotonView>().Owner);

			i++;
		}

		foreach (Shipyard shipyard in Shipyard.ActiveShipyards.Values)
		{
			if (shipyard == null) continue;

			Vector2 relativePosition = (Vector2) shipyard.transform.position - centerOfMass;

			GameObject ping = GetOrCreatePing(i);

			ping.transform.localPosition = relativePosition * Scale;
			ping.transform.rotation = Quaternion.identity;
			ping.GetComponent<Image>().sprite = ShipyardPingSprite;
			ping.GetComponent<Image>().color = PhotonTeamManager.GetTeamColor(shipyard.TeamIndex);

			i++;
		}

		for (; i < _pings.Count; i++)
		{
			_pings[i].SetActive(false);
		}
	}

	private GameObject GetOrCreatePing(int i)
	{
		if (i < _pings.Count)
		{
			GameObject ping = _pings[i];
			ping.SetActive(true);
			return ping;
		}
		else
		{
			GameObject ping = Instantiate(RadarPingPrefab, transform);
			_pings.Add(ping);
			return ping;
		}
	}
}
}