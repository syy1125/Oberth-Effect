using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Simulation.Vehicle;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public abstract class ResourceGeneratorBlock : MonoBehaviour
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

	/// <remark>
	/// The return value on this should NOT be time-scaled.
	/// </remark>
	public abstract Dictionary<VehicleResource, float> GetGenerationRate();
}
}