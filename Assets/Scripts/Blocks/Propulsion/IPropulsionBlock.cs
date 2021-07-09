using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public interface IPropulsionBlockRegistry : IBlockRegistry<IPropulsionBlock>, IEventSystemHandler
{}

public interface IPropulsionBlock
{
	void SetFuelPropulsionActive(bool fuelActive);
	
	void SetPropulsionCommands(Vector2 translateCommand, float rotateCommand);

	float GetMaxPropulsionForce(CardinalDirection localDirection);

	Dictionary<VehicleResource, float> GetMaxResourceUseRate();
}
}