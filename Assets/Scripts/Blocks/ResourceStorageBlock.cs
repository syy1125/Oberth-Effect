using System;
using Syy1125.OberthEffect.Simulation;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class ResourceStorageBlock : MonoBehaviour
{
	public float FuelCapacity;
	public float EnergyCapacity;

	private void OnEnable()
	{
		var manager = GetComponentInParent<VehicleResourceManager>();
		if (manager != null)
		{
			manager.AddStorage(this);
		}
	}

	private void OnDisable()
	{
		var manager = GetComponentInParent<VehicleResourceManager>();
		if (manager != null)
		{
			manager.RemoveStorage(this);
		}
	}
}
}