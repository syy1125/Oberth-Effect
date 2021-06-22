using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer.Config
{
public class VehicleConfig : MonoBehaviour
{
	[Header("References")]
	public VehicleDesigner Designer;

	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;

	private ColorContext _context;

	#region Unity Lifecycle

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
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
		ColorScheme colorScheme = ColorScheme.FromBlueprint(Designer.Blueprint);

		PrimaryColorPicker.InitColor(colorScheme.PrimaryColor);
		SecondaryColorPicker.InitColor(colorScheme.SecondaryColor);
		TertiaryColorPicker.InitColor(colorScheme.TertiaryColor);
		_context.SetColorScheme(colorScheme);
	}

	#region Event Listeners

	private void SetPrimaryColor(Color color)
	{
		_context.SetPrimaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.PRIMARY_COLOR, JsonUtility.ToJson(color));
	}

	private void SetSecondaryColor(Color color)
	{
		_context.SetSecondaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.SECONDARY_COLOR, JsonUtility.ToJson(color));
	}

	private void SetTertiaryColor(Color color)
	{
		_context.SetTertiaryColor(color);
		PlayerPrefs.SetString(PropertyKeys.TERTIARY_COLOR, JsonUtility.ToJson(color));
	}

	#endregion
}
}