using System;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class BlockHealthBar : MonoBehaviour
{
	public BlockCore Target;
	public Image HealthBar;
	public Gradient ColorGradient;

	private void LateUpdate()
	{
		float healthFraction = Target.HealthFraction;
		HealthBar.fillAmount = healthFraction;
		HealthBar.color = ColorGradient.Evaluate(healthFraction);

		transform.position = Target.transform.parent.TransformPoint(Target.CenterOfMassPosition);
	}
}
}