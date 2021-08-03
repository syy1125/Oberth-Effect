using System.Collections.Generic;
using Syy1125.OberthEffect.Common.Enums;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IWeaponEffectEmitter
{
	// Called from whatever's firing the weapon to ensure ordering of events.
	// For example, rotate the turret before applying the effect.
	void EmitterFixedUpdate(bool firing, bool isMine);
	IReadOnlyDictionary<string, float> GetResourceConsumptionRateRequest();
	void SatisfyResourceRequestAtLevel(float level);
	IReadOnlyDictionary<DamageType, float> GetMaxFirepower();
	string GetEmitterTooltip();
}
}