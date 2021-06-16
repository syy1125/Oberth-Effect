using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IPropulsionBlockRegistry : IBlockRegistry<IPropulsionBlock>, IEventSystemHandler
{}

public struct PropulsionRequest
{
	public Dictionary<VehicleResource, float> ResourceConsumptionRateRequest;
	public Vector2 ForceOrigin;
	public Vector2 Force;
}

public interface IPropulsionBlock
{
	PropulsionRequest GetResponse(float forwardBackCommand, float strafeCommand, float rotateCommand);
	void PlayEffect(PropulsionRequest request, float satisfaction);
}
}