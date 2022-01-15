using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Components.UserInterface;
using UnityEngine;

namespace Syy1125.OberthEffect.MainMenu.Options
{
public class OptionsScreen : MonoBehaviour
{
	public SwitchSelect UnitModeSwitch;
	public PercentageSlider DesignerGridOpacitySlider;
	public PercentageSlider ScreenShakeMultiplierSlider;

	private void Start()
	{
		UnitModeSwitch.Value = (int) PhysicsUnitUtils.UnitMode;
		UnitModeSwitch.OnValueChanged.AddListener(HandleUnitModeChange);

		float gridOpacity = PlayerPrefs.GetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, 0.2f);
		DesignerGridOpacitySlider.Value = gridOpacity;
		DesignerGridOpacitySlider.OnChange.AddListener(HandleGridOpacityChange);

		float screenShakeMultiplier = PlayerPrefs.GetFloat(PropertyKeys.SCREEN_SHAKE_MULTIPLIER, 1f);
		ScreenShakeMultiplierSlider.Value = screenShakeMultiplier;
		ScreenShakeMultiplierSlider.OnChange.AddListener(HandleScreenShakeMultiplierChange);
	}

	private void HandleUnitModeChange(int unitMode)
	{
		PhysicsUnitUtils.SetPhysicsUnitMode((PhysicsUnitMode) unitMode);
	}

	private void HandleGridOpacityChange(float opacity)
	{
		PlayerPrefs.SetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, opacity);
	}

	private void HandleScreenShakeMultiplierChange(float multiplier)
	{
		PlayerPrefs.SetFloat(PropertyKeys.SCREEN_SHAKE_MULTIPLIER, multiplier);
	}
}
}