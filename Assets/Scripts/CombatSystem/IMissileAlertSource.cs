using UnityEngine;

namespace Syy1125.OberthEffect.CombatSystem
{
/// <summary>
/// Something that generates a missile alert. This is decoupled from actual missiles, as actual missiles are mod content territory.
/// If they have a point defense target component, missile alert sources are always included in point defense calculations regardless of range.
/// </summary>
public interface IMissileAlertSource
{
	bool isActiveAndEnabled { get; }
	Transform transform { get; }
	float? GetHitTime();
	T GetComponent<T>() where T : Component;
}

public interface IMissileAlertReceiver
{
	void AddIncomingMissile(IMissileAlertSource missile);
	void RemoveIncomingMissile(IMissileAlertSource missile);
}
}