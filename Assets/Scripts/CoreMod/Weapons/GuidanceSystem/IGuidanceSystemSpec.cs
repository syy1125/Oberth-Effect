using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem
{
public interface IGuidanceSystemSpec
{
	string GetGuidanceSystemTooltip();
	float GetMaxRange(float initialSpeed, float lifetime);

	Vector2? GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	);
}
}