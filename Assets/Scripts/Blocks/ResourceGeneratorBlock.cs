using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class ResourceGeneratorBlock : MonoBehaviour
{
	private void OnEnable()
	{
		var manager = GetComponentInParent<VehicleResourceManager>();
		if (manager != null)
		{
			manager.AddGenerator(this);
		}
	}

	private void OnDisable()
	{
		var manager = GetComponentInParent<VehicleResourceManager>();
		if (manager != null)
		{
			manager.RemoveGenerator(this);
		}
	}

	// The return value on this should be time-scaled.
	public virtual Dictionary<VehicleResource, float> GenerateResources()
	{
		return null;
	}
}
}