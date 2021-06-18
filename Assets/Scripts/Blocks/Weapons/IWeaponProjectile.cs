using System;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public interface IWeaponProjectile : IEventSystemHandler
{
	IWeaponSystem From { get; set; }
}
}