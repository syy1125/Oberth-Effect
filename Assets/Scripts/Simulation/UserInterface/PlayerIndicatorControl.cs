using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class PlayerIndicatorControl : MonoBehaviour
{
	public GameObject PlayerIndicatorPrefab;

	private HashSet<VehicleCore> _unpairedVehicles;
	private Dictionary<VehicleCore, GameObject> _indicators;

	private void Awake()
	{
		_unpairedVehicles = new HashSet<VehicleCore>();
		_indicators = new Dictionary<VehicleCore, GameObject>();
	}

	private void Update()
	{
		_unpairedVehicles.Clear();
		_unpairedVehicles.UnionWith(VehicleCore.ActiveVehicles);

		foreach (KeyValuePair<VehicleCore, GameObject> entry in _indicators.ToList())
		{
			_unpairedVehicles.Remove(entry.Key);

			if (entry.Key.IsDead)
			{
				Destroy(entry.Value);
				_indicators.Remove(entry.Key);
			}
		}

		foreach (VehicleCore vehicle in _unpairedVehicles)
		{
			GameObject indicator = Instantiate(PlayerIndicatorPrefab, transform);
			indicator.GetComponent<HighlightTarget>().Target = vehicle.transform;
			_indicators.Add(vehicle, indicator);
		}
	}
}
}