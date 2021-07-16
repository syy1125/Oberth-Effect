using Syy1125.OberthEffect.Common.ColorScheme;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public interface IBeamWeaponVisualEffect : IEventSystemHandler
{
	void SetColorScheme(ColorScheme colors);
	
	void SetBeamPoints(Vector3 begin, Vector3 end, bool hit);
}
}