using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class ResourceStorageBlock : MonoBehaviour
{
	public ResourceEntry[] ResourceCapacities;

	public Dictionary<VehicleResource, float> ResourceCapacityDict { get; private set; }

	private void OnEnable()
	{
		var manager = GetComponentInParent<VehicleResourceManager>();
		if (manager != null)
		{
			manager.AddStorage(this);
		}
	}

	private void Start()
	{
		ResourceCapacityDict = new Dictionary<VehicleResource, float>(ResourceCapacities.Length);
		foreach (ResourceEntry entry in ResourceCapacities)
		{
			ResourceCapacityDict.Add(entry.Resource, entry.Amount);
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