using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public interface ITargetLockInfoProvider
{
	public string GetName();
	public Vector2 GetPosition();
	public Vector2 GetVelocity();
}
}