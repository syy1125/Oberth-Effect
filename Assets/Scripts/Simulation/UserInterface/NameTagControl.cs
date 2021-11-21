using System.Collections.Generic;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class NameTagControl : MonoBehaviour
{
	public GameObject NameTagPrefab;

	private HashSet<VehicleCore> _assignedVehicles;

	private void Awake()
	{
		_assignedVehicles = new HashSet<VehicleCore>();
	}

	private void Update()
	{
		foreach (VehicleCore vehicle in VehicleCore.ActiveVehicles)
		{
			if (_assignedVehicles.Add(vehicle))
			{
				GameObject nameTag = Instantiate(NameTagPrefab, transform);
				nameTag.GetComponent<NameTag>().Target = vehicle;
			}
		}
	}
}
}