using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class CraftConfig : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;

	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;

	private ColorContext _context;

	private bool _initializing;

	#region Unity Lifecycle

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
		_initializing = false;
	}

	private void OnEnable()
	{
		PrimaryColorPicker.OnChange.AddListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.AddListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.AddListener(SetTertiaryColor);
	}

	private void Start()
	{
		ReloadVehicle();
	}

	private void OnDisable()
	{
		PrimaryColorPicker.OnChange.RemoveListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.RemoveListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.RemoveListener(SetTertiaryColor);
	}

	#endregion

	public void ReloadVehicle()
	{
		_initializing = true;

		ColorScheme colorScheme = ColorScheme.FromBlueprint(Designer.Blueprint);

		PrimaryColorPicker.InitColor(colorScheme.PrimaryColor);
		SecondaryColorPicker.InitColor(colorScheme.SecondaryColor);
		TertiaryColorPicker.InitColor(colorScheme.TertiaryColor);
		_context.SetColorScheme(colorScheme);

		_initializing = false;
	}

	#region Event Listeners

	private void SetPrimaryColor(Color color)
	{
		if (_initializing) return;
		_context.SetPrimaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.PRIMARY_COLOR, JsonUtility.ToJson(color));
	}

	private void SetSecondaryColor(Color color)
	{
		if (_initializing) return;
		_context.SetSecondaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.SECONDARY_COLOR, JsonUtility.ToJson(color));
	}

	private void SetTertiaryColor(Color color)
	{
		if (_initializing) return;
		_context.SetTertiaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.TERTIARY_COLOR, JsonUtility.ToJson(color));
	}

	#endregion
}
}