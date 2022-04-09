using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IWeaponEffectEmitter
{
	public bool enabled { get; }
	public int? TargetPhotonId { get; set; }
	void SetAimPoint(Vector2? aimPoint);

	public Vector2? GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	);

	// Called from whatever's firing the weapon to ensure ordering of events.
	// For example, rotate the turret before applying the effect.
	void EmitterFixedUpdate(bool isMine, bool firing);
	IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();
	void SatisfyResourceRequestAtLevel(float level);
	float GetMaxRange();
	void GetMaxFirepower(IList<FirepowerEntry> entries);
	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
	string GetEmitterTooltip();
}
}