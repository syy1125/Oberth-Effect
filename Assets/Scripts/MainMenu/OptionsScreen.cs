using System;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Common.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.MainMenu
{
public class OptionsScreen : MonoBehaviour
{
	public PercentageSlider DesignerGridOpacitySlider;

	private void Start()
	{
		float gridOpacity = PlayerPrefs.GetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, 0.2f);
		DesignerGridOpacitySlider.Value = gridOpacity;
		DesignerGridOpacitySlider.OnChange.AddListener(HandleGridOpacityChange);
	}

	private void HandleGridOpacityChange(float opacity)
	{
		PlayerPrefs.SetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, opacity);
	}
}
}