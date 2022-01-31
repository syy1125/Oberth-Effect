using System;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class AlertDisplay : MonoBehaviour
{
	public VehicleWeaponControl WeaponControl;
	public Text MissileCountDisplay;
	public Text MissileTimerDisplay;

	private void LateUpdate()
	{
		UpdateMissileDisplay();
	}

	private void UpdateMissileDisplay()
	{
		if (WeaponControl == null || !WeaponControl.isActiveAndEnabled)
		{
			MissileCountDisplay.gameObject.SetActive(false);
			MissileTimerDisplay.gameObject.SetActive(false);
			return;
		}

		if (WeaponControl.IncomingMissiles.Count == 0)
		{
			MissileCountDisplay.gameObject.SetActive(false);
			MissileTimerDisplay.gameObject.SetActive(false);
		}
		else
		{
			Color textColor = Time.unscaledTime % 1 >= 0.5f ? Color.red : Color.white;

			MissileCountDisplay.gameObject.SetActive(true);
			MissileCountDisplay.text = WeaponControl.IncomingMissiles.Count > 1
				? $"< {WeaponControl.IncomingMissiles.Count} Incoming Missiles >"
				: "< Incoming Missile >";
			MissileCountDisplay.color = textColor;

			float? minTime = null;

			foreach (var missile in WeaponControl.IncomingMissiles)
			{
				float? missileTime = missile.GetHitTime();

				if (missileTime != null)
				{
					if (minTime == null || minTime.Value > missileTime.Value)
					{
						minTime = missileTime;
					}
				}
			}

			if (minTime != null)
			{
				MissileTimerDisplay.gameObject.SetActive(true);
				MissileTimerDisplay.text = $"< Time to Impact {minTime.Value:0.00}s >";
				MissileTimerDisplay.color = textColor;
			}
			else
			{
				MissileTimerDisplay.gameObject.SetActive(false);
			}
		}
	}
}
}