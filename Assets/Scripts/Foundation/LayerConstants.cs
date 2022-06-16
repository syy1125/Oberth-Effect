﻿using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
{
public static class LayerConstants
{
	public const int VEHICLE_BLOCK_LAYER = 6;
	public const int CELESTIAL_BODY_LAYER = 7;
	public const int VEHICLE_SHIELD_LAYER = 8;
	public const int WEAPON_PROJECTILE_LAYER = 11;
	public const int SHIPYARD_LAYER = 12;

	public const int VEHICLE_LAYER_MASK =
		(1 << VEHICLE_BLOCK_LAYER)
		| (1 << VEHICLE_SHIELD_LAYER);
	public const int DAMAGEABLE_LAYER_MASK =
		(1 << VEHICLE_BLOCK_LAYER)
		| (1 << CELESTIAL_BODY_LAYER)
		| (1 << VEHICLE_SHIELD_LAYER)
		| (1 << WEAPON_PROJECTILE_LAYER)
		| (1 << SHIPYARD_LAYER);

	public static readonly ContactFilter2D WeaponHitFilter = new()
	{
		layerMask = DAMAGEABLE_LAYER_MASK,
		useLayerMask = true,
		useTriggers = true, // For PD
	};
}
}