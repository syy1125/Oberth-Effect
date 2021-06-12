using System.Collections.Generic;
using Syy1125.OberthEffect.Common;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public interface IResourceConsumer
{
	// Bigger number as higher priority
	int GetResourcePriority();

	Dictionary<VehicleResource, float> GetResourceRequests();

	// Input ranges from 0 to 1
	void SetSatisfactionLevel(float satisfaction);
}
}