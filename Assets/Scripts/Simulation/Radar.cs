using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Simulation.Vehicle;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation
{
public class Radar : MonoBehaviour
{
	public GameObject VehiclePingPrefab;
	public Rigidbody2D OwnVehicle;
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
			Vector2 relativePosition = vehicle.GetComponent<Rigidbody2D>().worldCenterOfMass - centerOfMass;

			GameObject ping = GetOrCreatePing(i);
			
			ping.transform.localPosition = relativePosition * Scale;
			ping.transform.rotation = vehicle.transform.rotation;
			ping.GetComponent<Image>().color =
				PhotonTeamManager.GetPlayerTeamColor(vehicle.GetComponent<PhotonView>().Owner);

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
			GameObject ping = Instantiate(VehiclePingPrefab, transform);
			_pings.Add(ping);
			return ping;
		}
	}
}
}