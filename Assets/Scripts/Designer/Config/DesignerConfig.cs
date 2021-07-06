using System;
using System.Linq;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Config
{
public class DesignerConfig : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;

	[Header("Vehicle Config")]
	public SwitchSelect ControlModeSelect;
	public Toggle CustomColorToggle;
	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;

	private VehicleBlueprint Blueprint => Designer.Blueprint;

	private ColorContext _context;

	#region Unity Lifecycle

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
	}

	private void OnEnable()
	{
		ControlModeSelect.OnValueChanged.AddListener(SetControlMode);
		CustomColorToggle.onValueChanged.AddListener(SetUseCustomColor);
		PrimaryColorPicker.OnChange.AddListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.AddListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.AddListener(SetTertiaryColor);
	}

	private void Start()
	{
		ControlModeSelect.SetOptions(Enum.GetNames(typeof(VehicleControlMode)));
	}

	private void OnDisable()
	{
		ControlModeSelect.OnValueChanged.RemoveListener(SetControlMode);
		CustomColorToggle.onValueChanged.RemoveListener(SetUseCustomColor);
		PrimaryColorPicker.OnChange.RemoveListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.RemoveListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.RemoveListener(SetTertiaryColor);
	}

	#endregion

	public void ReloadVehicle()
	{
		ControlModeSelect.Value = (int) Blueprint.DefaultControlMode;

		ColorScheme colorScheme = ColorScheme.FromBlueprint(Designer.Blueprint);

		CustomColorToggle.isOn = Blueprint.UseCustomColors;

		PrimaryColorPicker.InitColor(colorScheme.PrimaryColor);
		SecondaryColorPicker.InitColor(colorScheme.SecondaryColor);
		TertiaryColorPicker.InitColor(colorScheme.TertiaryColor);

		_context.SetColorScheme(colorScheme);
	}

	#region Vehicle Config Event Listeners

	private void SetControlMode(int modeIndex)
	{
		Blueprint.DefaultControlMode = (VehicleControlMode) modeIndex;
	}

	private void SetUseCustomColor(bool useCustomColors)
	{
		Blueprint.ColorScheme = ColorScheme.PlayerColorScheme();
		Blueprint.UseCustomColors = useCustomColors;

		ReloadVehicle();
	}

	private void SetPrimaryColor(Color color)
	{
		_context.SetPrimaryColor(color);

		if (Blueprint.UseCustomColors)
		{
			Blueprint.ColorScheme.PrimaryColor = color;
		}
		else
		{
			PlayerPrefs.SetString(PropertyKeys.PRIMARY_COLOR, JsonUtility.ToJson(color));
		}
	}

	private void SetSecondaryColor(Color color)
	{
		_context.SetSecondaryColor(color);

		if (Blueprint.UseCustomColors)
		{
			Blueprint.ColorScheme.SecondaryColor = color;
		}
		else
		{
			PlayerPrefs.SetString(PropertyKeys.SECONDARY_COLOR, JsonUtility.ToJson(color));
		}
	}

	private void SetTertiaryColor(Color color)
	{
		_context.SetTertiaryColor(color);

		if (Blueprint.UseCustomColors)
		{
			Blueprint.ColorScheme.TertiaryColor = color;
		}
		else
		{
			PlayerPrefs.SetString(PropertyKeys.TERTIARY_COLOR, JsonUtility.ToJson(color));
		}
	}

	#endregion
}
}