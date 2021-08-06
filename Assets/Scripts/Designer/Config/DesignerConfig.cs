using System;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Config
{
public class DesignerConfig : MonoBehaviour
{
	[Header("Input")]
	public InputActionReference SelectBlockAction;
	public InputActionReference DeselectAction;

	[Header("References")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;
	public DesignerAreaMask AreaMask;
	public Transform SelectionIndicator;
	public Text StatusText;
	public RectTransform ConfigParent;

	[Header("Vehicle Config")]
	public SwitchSelect ControlModeSelect;
	public Toggle CustomColorToggle;
	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;

	[Header("Engine Config")]
	public Toggle EngineTranslationToggle;
	public Toggle EngineRotationToggle;

	private VehicleBlueprint Blueprint => Designer.Blueprint;

	private ColorContext _context;
	private Vector2Int? _selectedLocation;
	private bool _updatingElements;

	#region Unity Lifecycle

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
	}

	private void OnEnable()
	{
		EnableActions();
		AttachVehicleConfigListeners();
		AttachEngineConfigListeners();

		_selectedLocation = null;
		ShowVehicleConfig();
	}

	private void EnableActions()
	{
		SelectBlockAction.action.Enable();
		DeselectAction.action.Enable();
		SelectBlockAction.action.performed += HandleSelect;
		DeselectAction.action.performed += HandleDeselect;
	}

	private void AttachVehicleConfigListeners()
	{
		ControlModeSelect.OnValueChanged.AddListener(SetControlMode);
		CustomColorToggle.onValueChanged.AddListener(SetUseCustomColor);
		PrimaryColorPicker.OnChange.AddListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.AddListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.AddListener(SetTertiaryColor);
	}

	private void AttachEngineConfigListeners()
	{
		EngineTranslationToggle.onValueChanged.AddListener(SetEngineRespondToTranslation);
		EngineRotationToggle.onValueChanged.AddListener(SetEngineRespondToRotation);
	}

	private void Start()
	{
		ControlModeSelect.SetOptions(Enum.GetNames(typeof(VehicleControlMode)));
	}

	private void OnDisable()
	{
		DisableActions();
		DetachVehicleConfigListeners();
		DetachEngineConfigListeners();

		_selectedLocation = null;
		SelectionIndicator.gameObject.SetActive(false);
	}

	private void DisableActions()
	{
		SelectBlockAction.action.performed -= HandleSelect;
		DeselectAction.action.performed -= HandleDeselect;
		SelectBlockAction.action.Disable();
		DeselectAction.action.Disable();
	}

	private void DetachVehicleConfigListeners()
	{
		ControlModeSelect.OnValueChanged.RemoveListener(SetControlMode);
		CustomColorToggle.onValueChanged.RemoveListener(SetUseCustomColor);
		PrimaryColorPicker.OnChange.RemoveListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.RemoveListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.RemoveListener(SetTertiaryColor);
	}

	private void DetachEngineConfigListeners()
	{
		EngineTranslationToggle.onValueChanged.RemoveListener(SetEngineRespondToTranslation);
		EngineRotationToggle.onValueChanged.RemoveListener(SetEngineRespondToRotation);
	}

	#endregion

	#region Input Event Handlers

	private void HandleSelect(InputAction.CallbackContext context)
	{
		if (AreaMask.Hovering && Builder.HasBlockAt(Designer.HoverPositionInt))
		{
			_selectedLocation = Designer.HoverPositionInt;
			ShowBlockConfig();
		}
	}

	private void HandleDeselect(InputAction.CallbackContext context)
	{
		_selectedLocation = null;
		ShowVehicleConfig();
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

	private void ShowVehicleConfig()
	{
		Debug.Assert(_selectedLocation == null, nameof(_selectedLocation) + " == null");

		StatusText.text = string.Join(
			"\n",
			"Showing vehicle config",
			"Click on a block to view its config"
		);

		SetVehicleConfigEnabled(true);
		SetEngineConfigEnabled(false);

		LayoutRebuilder.MarkLayoutForRebuild(ConfigParent);

		SelectionIndicator.gameObject.SetActive(false);
	}

	private void ShowBlockConfig()
	{
		Debug.Assert(_selectedLocation != null, nameof(_selectedLocation) + " != null");

		VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(_selectedLocation.Value);
		GameObject blockObject = Builder.GetBlockObjectAt(_selectedLocation.Value);
		BlockSpec blockSpec = BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec;

		StatusText.text = string.Join(
			"\n",
			$"Configuring {blockSpec.Info.FullName}",
			"Press 'Q' to show vehicle config"
		);

		SetVehicleConfigEnabled(false);

		LinearEngine engine = blockObject.GetComponent<LinearEngine>();
		SetEngineConfigEnabled(engine != null);
		if (engine != null) UpdateEngineConfigElements(engine);

		LayoutRebuilder.MarkLayoutForRebuild(ConfigParent);

		BoundsInt blockBounds = TransformUtils.TransformBounds(
			new BlockBounds(blockSpec.Construction.BoundsMin, blockSpec.Construction.BoundsMax).ToBoundsInt(),
			new Vector2Int(blockInstance.X, blockInstance.Y), blockInstance.Rotation
		);

		SelectionIndicator.localPosition = blockBounds.center - new Vector3(0.5f, 0.5f, 0f);
		SelectionIndicator.localScale = blockBounds.size;
		SelectionIndicator.gameObject.SetActive(true);
	}

	private void SetVehicleConfigEnabled(bool configEnabled)
	{
		ControlModeSelect.gameObject.SetActive(configEnabled);
		CustomColorToggle.gameObject.SetActive(configEnabled);
		PrimaryColorPicker.gameObject.SetActive(configEnabled);
		SecondaryColorPicker.gameObject.SetActive(configEnabled);
		TertiaryColorPicker.gameObject.SetActive(configEnabled);
	}

	private void SetEngineConfigEnabled(bool configEnabled)
	{
		EngineTranslationToggle.gameObject.SetActive(configEnabled);
		EngineRotationToggle.gameObject.SetActive(configEnabled);
	}

	private void UpdateEngineConfigElements(LinearEngine engine)
	{
		_updatingElements = true;

		EngineTranslationToggle.isOn = engine.RespondToTranslation;
		EngineRotationToggle.isOn = engine.RespondToRotation;

		_updatingElements = false;
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

	#region Engine Config Event Listeners

	private void SetEngineRespondToTranslation(bool engineTranslation)
	{
		if (_updatingElements) return;
		Debug.Assert(_selectedLocation != null, nameof(_selectedLocation) + " != null");

		VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(_selectedLocation.Value);
		GameObject blockObject = Builder.GetBlockObjectAt(_selectedLocation.Value);

		blockObject.GetComponent<LinearEngine>().RespondToTranslation = engineTranslation;
		BlockConfigHelper.SaveConfig(blockInstance, blockObject);
	}

	private void SetEngineRespondToRotation(bool engineRotation)
	{
		if (_updatingElements) return;
		Debug.Assert(_selectedLocation != null, nameof(_selectedLocation) + " != null");

		VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(_selectedLocation.Value);
		GameObject blockObject = Builder.GetBlockObjectAt(_selectedLocation.Value);

		blockObject.GetComponent<LinearEngine>().RespondToRotation = engineRotation;
		BlockConfigHelper.SaveConfig(blockInstance, blockObject);
	}

	#endregion
}
}