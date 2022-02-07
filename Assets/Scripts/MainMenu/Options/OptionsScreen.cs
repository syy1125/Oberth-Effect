using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Init;
using UnityEngine;

namespace Syy1125.OberthEffect.MainMenu.Options
{
public class OptionsScreen : MonoBehaviour
{
	public PercentageSlider MasterVolumeSlider;
	public PercentageSlider UIVolumeSlider;
	public PercentageSlider BlocksVolumeSlider;

	public SwitchSelect UnitModeSwitch;
	public PercentageSlider DesignerGridOpacitySlider;
	public PercentageSlider ScreenShakeMultiplierSlider;

	private void Start()
	{
		MasterVolumeSlider.Value = AudioMixerManager.Instance.GetVolume(PropertyKeys.MASTER_VOLUME);
		MasterVolumeSlider.OnChange.AddListener(HandleMasterVolumeChange);

		UIVolumeSlider.Value = AudioMixerManager.Instance.GetVolume(PropertyKeys.UI_VOLUME);
		UIVolumeSlider.OnChange.AddListener(HandleUIVolumeChange);

		BlocksVolumeSlider.Value = AudioMixerManager.Instance.GetVolume(PropertyKeys.BLOCKS_VOLUME);
		BlocksVolumeSlider.OnChange.AddListener(HandleBlocksVolumeChange);

		UnitModeSwitch.Value = (int) PhysicsUnitUtils.UnitMode;
		UnitModeSwitch.OnValueChanged.AddListener(HandleUnitModeChange);

		float gridOpacity = PlayerPrefs.GetFloat(PropertyKeys.DESIGNER_GRID_OPACITY, 0.2f);
		DesignerGridOpacitySlider.Value = gridOpacity;
		DesignerGridOpacitySlider.OnChange.AddListener(HandleGridOpacityChange);

		float screenShakeMultiplier = PlayerPrefs.GetFloat(PropertyKeys.SCREEN_SHAKE_MULTIPLIER, 1f);
		ScreenShakeMultiplierSlider.Value = screenShakeMultiplier;
		ScreenShakeMultiplierSlider.OnChange.AddListener(HandleScreenShakeMultiplierChange);
	}

	private void HandleMasterVolumeChange(float volume)
	{
		AudioMixerManager.Instance.SetVolume(PropertyKeys.MASTER_VOLUME, volume);
	}

	private void HandleUIVolumeChange(float volume)
	{
		AudioMixerManager.Instance.SetVolume(PropertyKeys.UI_VOLUME, volume);
	}

	private void HandleBlocksVolumeChange(float volume)
	{
		AudioMixerManager.Instance.SetVolume(PropertyKeys.BLOCKS_VOLUME, volume);
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