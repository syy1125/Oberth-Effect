using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Propulsion
{
public interface IPropulsionBlockRegistry : IBlockRegistry<IPropulsionBlock>, IEventSystemHandler
{}

public interface IPropulsionBlock
{
	void SetPropulsionCommands(float forwardBackCommand, float strafeCommand, float rotateCommand);
}
}