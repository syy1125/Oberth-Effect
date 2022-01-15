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

	private void Start()
	{
		UnitModeSwitch.Value = (int) PhysicsUnitUtils.UnitMode;
		UnitModeSwitch.OnValueChanged.AddListener(HandleUnitModeChange);

		float gridOpacity = PlayerPrefs.GetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, 0.2f);
		DesignerGridOpacitySlider.Value = gridOpacity;
		DesignerGridOpacitySlider.OnChange.AddListener(HandleGridOpacityChange);
	}

	private void HandleUnitModeChange(int unitMode)
	{
		PhysicsUnitUtils.SetPhysicsUnitMode((PhysicsUnitMode) unitMode);
	}

	private void HandleGridOpacityChange(float opacity)
	{
		PlayerPrefs.SetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, opacity);
	}
}
}