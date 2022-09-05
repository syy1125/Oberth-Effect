namespace Syy1125.OberthEffect.Blocks
{
public enum BlockEnvironment
{
	Palette, // Block is instantiated in palette
	Preview, // Block is instantiated in preview when placing a block
	Designer, // Block is instantiated in designer as part of the vehicle
	Simulation // Block is instantiated in simulation
}

public struct BlockContext
{
	public bool IsMainVehicle;
	public BlockEnvironment Environment;
}
}