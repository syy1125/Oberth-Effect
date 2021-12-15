using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IWeaponEffectEmitter
{
	void SetAimPoint(Vector2? aimPoint);
	// Called from whatever's firing the weapon to ensure ordering of events.
	// For example, rotate the turret before applying the effect.
	void EmitterFixedUpdate(bool isMine, bool firing);
	IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();
	void SatisfyResourceRequestAtLevel(float level);
	IReadOnlyDictionary<DamageType, float> GetMaxFirepower();
	IReadOnlyDictionary<string, float> GetMaxResourceUseRate();
	string GetEmitterTooltip();
}
}