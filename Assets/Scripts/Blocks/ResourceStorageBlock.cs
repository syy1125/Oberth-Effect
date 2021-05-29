using Syy1125.OberthEffect.Simulation;
using Syy1125.OberthEffect.Simulation.Vehicle;
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