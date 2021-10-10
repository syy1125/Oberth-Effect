namespace Syy1125.OberthEffect.Common
{
public static class LayerConstants
{
	public const int VEHICLE_BLOCK_LAYER = 6;
	public const int CELESTIAL_BODY_LAYER = 7;
	public const int VEHICLE_SHIELD_LAYER = 8;

	public const int VEHICLE_LAYER_MASK =
		(1 << LayerConstants.VEHICLE_BLOCK_LAYER)
		| (1 << LayerConstants.VEHICLE_SHIELD_LAYER);
	public const int DAMAGEABLE_LAYER_MASK =
		(1 << LayerConstants.VEHICLE_BLOCK_LAYER)
		| (1 << LayerConstants.CELESTIAL_BODY_LAYER)
		| (1 << LayerConstants.VEHICLE_SHIELD_LAYER);
}
}