using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public interface IPropulsionBlockRegistry : IBlockRegistry<IPropulsionBlock>, IEventSystemHandler
{}

public interface IPropulsionBlock
{
	void SetPropulsionCommands(Vector2 translateCommand, float rotateCommand);

	Vector2 GetPropulsionForceOrigin();

	float GetMaxPropulsionForce(CardinalDirection localDirection);

	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
}
}