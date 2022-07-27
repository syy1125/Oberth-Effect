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
	T GetComponent<T>();
}

public interface IMissileAlertReceiver
{
	void AddIncomingMissile(IMissileAlertSource missile);
	void RemoveIncomingMissile(IMissileAlertSource missile);
}

public static class MissileAlertSystem
{
	public static void OnTargetChanged(
		IMissileAlertSource source, IGuidedWeaponTarget prevTarget, IGuidedWeaponTarget nextTarget
	)
	{
		if (prevTarget == nextTarget) return;
		
		if (IsValidTarget(prevTarget))
		{
			foreach (var receiver in prevTarget.GetComponents<IMissileAlertReceiver>())
			{
				receiver.RemoveIncomingMissile(source);
			}
		}

		if (IsValidTarget(nextTarget))
		{
			foreach (var receiver in nextTarget.GetComponents<IMissileAlertReceiver>())
			{
				receiver.AddIncomingMissile(source);
			}
		}
	}

	private static bool IsValidTarget(IGuidedWeaponTarget target)
	{
		// Unity's nullability system doesn't play nice with System.Object.Equals
		return target != null && !target.Equals(null);
	}
}
}