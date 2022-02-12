using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public interface IPropulsionBlockRegistry : IBlockRegistry<IPropulsionBlock>
{}

public interface IPropulsionBlock
{
	bool RespondToTranslation { get; }
	bool RespondToRotation { get; }

	void SetPropulsionCommands(InputCommand horizontal, InputCommand vertical, InputCommand rotate);

	Vector2 GetPropulsionForceOrigin();

	float GetMaxPropulsionForce(CardinalDirection localDirection);

	float GetMaxFreeTorqueCcw();

	float GetMaxFreeTorqueCw();
}
}