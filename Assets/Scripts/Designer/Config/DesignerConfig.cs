﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Components.UserInterface;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Foundation.UserInterface;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Config
{
public class DesignerConfig : MonoBehaviour, IElementControllerContext
{
	[Header("Input")]
	public InputActionReference SelectBlockAction;
	public InputActionReference DeselectAction;
	public InputActionReference MultiSelectAction;

	[Header("References")]
	public VehicleDesigner Designer;
	public GameObject SelectionIndicatorPrefab;
	public Transform IndicatorParent;
	public Text StatusText;
	public RectTransform ConfigParent;

	[Header("Vehicle Config")]
	public SwitchSelect ControlModeSelect;
	public GameObject ColorSchemeTitle;
	public Toggle CustomColorToggle;
	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;
	public GameObject RotationPidTitle;
	public NumericSlider PidResponseSlider;
	public NumericSlider PidDerivativeTimeSlider;
	public NumericSlider PidIntegralTimeSlider;

	[Header("Block Config")]
	public GameObject TogglePrefab;
	public GameObject SwitchSelectPrefab;

	private VehicleBlueprint Blueprint => Designer.Blueprint;
	private VehicleBuilder Builder => Designer.Builder;
	private DesignerAreaMask AreaMask => Designer.AreaMask;

	private ColorContext _context;

	private VehicleBlueprint.BlockInstance _prevSelectBlock;
	private bool _additive;
	private List<VehicleBlueprint.BlockInstance> _selectedBlocks;
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _selectionIndicators;

	private HashSet<Type> _configComponents;
	private List<GameObject> _blockConfigItems;
	public bool UpdatingElements { get; private set; }

	public bool HasSelectedBlock => _selectedBlocks.Count > 0;

	#region Initialization

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
		_selectedBlocks = new();
		_selectionIndicators = new();
		_configComponents = new();
		_blockConfigItems = new();
	}

	private void OnEnable()
	{
		EnableActions();
		AttachVehicleConfigListeners();

		_selectedBlocks.Clear();
		ShowVehicleConfig();
	}

	private void EnableActions()
	{
		DeselectAction.action.performed += HandleDeselect;
	}

	private void AttachVehicleConfigListeners()
	{
		ControlModeSelect.OnValueChanged.AddListener(SetControlMode);
		CustomColorToggle.onValueChanged.AddListener(SetUseCustomColor);
		PrimaryColorPicker.OnChange.AddListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.AddListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.AddListener(SetTertiaryColor);
		PidResponseSlider.OnChange.AddListener(SetPidResponse);
		PidDerivativeTimeSlider.OnChange.AddListener(SetPidDerivativeTime);
		PidIntegralTimeSlider.OnChange.AddListener(SetPidIntegralTime);
	}

	private void Start()
	{
		ControlModeSelect.SetOptions(Enum.GetNames(typeof(VehicleControlMode)));
	}

	private void OnDisable()
	{
		DisableActions();
		DetachVehicleConfigListeners();

		_selectedBlocks.Clear();
		ClearSelectionIndicators();
	}

	private void DisableActions()
	{
		DeselectAction.action.performed -= HandleDeselect;
	}

	private void DetachVehicleConfigListeners()
	{
		ControlModeSelect.OnValueChanged.RemoveListener(SetControlMode);
		CustomColorToggle.onValueChanged.RemoveListener(SetUseCustomColor);
		PrimaryColorPicker.OnChange.RemoveListener(SetPrimaryColor);
		SecondaryColorPicker.OnChange.RemoveListener(SetSecondaryColor);
		TertiaryColorPicker.OnChange.RemoveListener(SetTertiaryColor);
		PidResponseSlider.OnChange.RemoveListener(SetPidResponse);
		PidDerivativeTimeSlider.OnChange.RemoveListener(SetPidDerivativeTime);
		PidIntegralTimeSlider.OnChange.RemoveListener(SetPidIntegralTime);
	}

	#endregion

	#region User Input

	private void Update()
	{
		if (!AreaMask.Hovering) return;

		if (SelectBlockAction.action.ReadValue<float>() > 0.5f)
		{
			HandleClickInput();
		}
		else
		{
			// Not clicking, idle
			_prevSelectBlock = null;
		}
	}

	private void HandleClickInput()
	{
		VehicleBlueprint.BlockInstance blockInstance = Builder.GetBlockInstanceAt(Designer.HoverPositionInt);

		if (MultiSelectAction.action.ReadValue<float>() > 0.5f)
		{
			// Holding shift, drag select multiple blocks
			if (blockInstance == null) return;

			if (_prevSelectBlock == null)
			{
				_additive = !_selectedBlocks.Remove(blockInstance);
				if (_additive) _selectedBlocks.Add(blockInstance);
				ShowAutoConfig();

				_prevSelectBlock = blockInstance;
			}
			else if (blockInstance != _prevSelectBlock)
			{
				if (_additive)
				{
					if (!_selectedBlocks.Contains(blockInstance))
					{
						_selectedBlocks.Add(blockInstance);
						ShowBlockConfig();
					}
				}
				else
				{
					if (_selectedBlocks.Remove(blockInstance)) ShowAutoConfig();
				}

				_prevSelectBlock = blockInstance;
			}
			// Otherwise, mouse is still over the block that the user previously clicked on.
			// Idle for now.
		}
		else
		{
			// Select single block
			if (_prevSelectBlock != null) return;
			_prevSelectBlock = blockInstance;

			if (blockInstance == null)
			{
				_selectedBlocks.Clear();
				ShowVehicleConfig();
			}
			else
			{
				_selectedBlocks.Clear();
				_selectedBlocks.Add(blockInstance);
				ShowBlockConfig();
			}
		}
	}

	private void HandleDeselect(InputAction.CallbackContext context)
	{
		_selectedBlocks.Clear();
		ShowVehicleConfig();
	}

	#endregion

	public void ReloadVehicle()
	{
		UpdatingElements = true;

		_selectedBlocks.Clear();
		ClearSelectionIndicators();

		ControlModeSelect.Value = (int) Blueprint.DefaultControlMode;

		ColorScheme colorScheme = ColorScheme.FromBlueprint(Blueprint);

		CustomColorToggle.isOn = Blueprint.UseCustomColors;

		PrimaryColorPicker.InitColor(colorScheme.PrimaryColor);
		SecondaryColorPicker.InitColor(colorScheme.SecondaryColor);
		TertiaryColorPicker.InitColor(colorScheme.TertiaryColor);

		PidResponseSlider.SetValue(Blueprint.PidConfig.Response);
		PidDerivativeTimeSlider.SetValue(Blueprint.PidConfig.DerivativeTime);
		PidIntegralTimeSlider.SetValue(Blueprint.PidConfig.IntegralTime);

		_context.SetColorScheme(colorScheme);

		ShowVehicleConfig();

		UpdatingElements = false;
	}

	private void ShowAutoConfig()
	{
		if (_selectedBlocks.Count > 0)
		{
			ShowBlockConfig();
		}
		else
		{
			ShowVehicleConfig();
		}
	}

	private void ShowVehicleConfig()
	{
		Debug.Assert(_selectedBlocks.Count == 0, nameof(_selectedBlocks) + ".Count == 0");

		StatusText.text = string.Join(
			"\n",
			"Showing vehicle config",
			"Click on a block to view its config"
		);

		SetVehicleConfigEnabled(true);
		_configComponents.Clear();
		ClearBlockConfigItems();

		LayoutRebuilder.MarkLayoutForRebuild(ConfigParent);

		foreach (GameObject indicator in _selectionIndicators.Values)
		{
			indicator.SetActive(false);
		}
	}

	private void ShowBlockConfig()
	{
		Debug.Assert(_selectedBlocks.Count > 0, nameof(_selectedBlocks) + ".Count > 0");

		SetVehicleConfigEnabled(false);
		UpdateSelectionIndicators();

		bool sameBlockId = true;
		HashSet<Type> configComponents = new HashSet<Type>(
			Builder.GetBlockObject(_selectedBlocks[0])
				.GetComponents<IConfigComponent>()
				.Select(component => component.GetType())
		);

		for (int i = 1; i < _selectedBlocks.Count; i++)
		{
			configComponents.IntersectWith(
				Builder.GetBlockObject(_selectedBlocks[i])
					.GetComponents<IConfigComponent>()
					.Select(component => component.GetType())
			);

			if (sameBlockId && _selectedBlocks[i].BlockId != _selectedBlocks[0].BlockId)
			{
				sameBlockId = false;
			}
		}

		if (configComponents.Count == 0)
		{
			StatusText.text = _selectedBlocks.Count == 1
				? "Selected block has no configurable components."
				: "Selected blocks have no common configurable components.";
			_configComponents.Clear();
			ClearBlockConfigItems();
			return;
		}

		StatusText.text = string.Join(
			"\n",
			_selectedBlocks.Count <= 1
				? $"Configuring {BlockDatabase.Instance.GetBlockSpec(_selectedBlocks[0].BlockId).Info.FullName} at {_selectedBlocks[0].Position}."
				: sameBlockId
					? $"Configuring {_selectedBlocks.Count} {BlockDatabase.Instance.GetBlockSpec(_selectedBlocks[0].BlockId).Info.FullName} blocks."
					: $"Configuring {_selectedBlocks.Count} blocks.",
			"Press 'Q' to show vehicle config"
		);

		if (configComponents.SetEquals(_configComponents))
		{
			UpdateBlockConfigItems();
		}
		else
		{
			_configComponents = configComponents;
			ClearBlockConfigItems();
			CreateBlockConfigItems();
		}
	}

	#region Config Display

	private void SetVehicleConfigEnabled(bool configEnabled)
	{
		ControlModeSelect.gameObject.SetActive(configEnabled);
		ColorSchemeTitle.SetActive(configEnabled);
		CustomColorToggle.gameObject.SetActive(configEnabled);
		PrimaryColorPicker.gameObject.SetActive(configEnabled);
		SecondaryColorPicker.gameObject.SetActive(configEnabled);
		TertiaryColorPicker.gameObject.SetActive(configEnabled);
		RotationPidTitle.SetActive(configEnabled);
		PidResponseSlider.gameObject.SetActive(configEnabled);
		PidDerivativeTimeSlider.gameObject.SetActive(configEnabled);
		PidIntegralTimeSlider.gameObject.SetActive(configEnabled);
	}

	private void ClearBlockConfigItems()
	{
		foreach (GameObject item in _blockConfigItems)
		{
			Destroy(item);
		}

		_blockConfigItems.Clear();
	}

	private void CreateBlockConfigItems()
	{
		Debug.Assert(_selectedBlocks.Count > 0, nameof(_selectedBlocks) + ".Count > 0");
		Debug.Assert(_blockConfigItems.Count == 0, nameof(_blockConfigItems) + ".Count == 0");

		List<JObject> configs = _selectedBlocks.Select(block => TypeUtils.ParseJson(block.Config)).ToList();

		foreach (
			IConfigComponent component in
			Builder.GetBlockObject(_selectedBlocks[0]).GetComponents<IConfigComponent>()
		)
		{
			Type componentType = component.GetType();
			if (!_configComponents.Contains(componentType)) continue;

			string configKey = TypeUtils.GetClassKey(componentType);
			var currentConfigs =
				configs.Select(config => config.ContainsKey(configKey) ? (JObject) config[configKey] : null).ToList();

			foreach (ConfigItemBase item in component.GetConfigItems())
			{
				GameObject row = null;

				switch (item)
				{
					case ToggleConfigItem toggleItem:
					{
						row = Instantiate(TogglePrefab, ConfigParent);

						var toggle = row.GetComponent<Toggle>();
						toggle.isOn = GetToggleValue(currentConfigs, toggleItem);
						toggle.onValueChanged.AddListener(
							value => SetBlockConfigs(componentType, toggleItem.Key, new JValue(value))
						);

						break;
					}
					case IntSwitchSelectConfigItem intSwitchItem:
					{
						row = Instantiate(SwitchSelectPrefab, ConfigParent);

						var switchSelect = row.GetComponent<SwitchSelect>();
						switchSelect.SetOptions(intSwitchItem.Options);
						switchSelect.Value = GetIntSwitchValue(currentConfigs, intSwitchItem);
						switchSelect.OnValueChanged.AddListener(
							value => SetBlockConfigs(componentType, intSwitchItem.Key, new JValue(value))
						);

						break;
					}
					case StringSwitchSelectConfigItem stringSwitchItem:
					{
						row = Instantiate(SwitchSelectPrefab, ConfigParent);

						var switchSelect = row.GetComponent<SwitchSelect>();
						switchSelect.SetOptions(stringSwitchItem.Options);
						switchSelect.Value = GetStringSwitchValue(currentConfigs, stringSwitchItem);
						switchSelect.OnValueChanged.AddListener(
							value => SetBlockConfigs(
								componentType, stringSwitchItem.Key, new JValue(stringSwitchItem.Serialize(value))
							)
						);

						break;
					}
					default:
					{
						Debug.LogError($"Unhandled config item type {item.GetType()}");
						break;
					}
				}

				if (row != null)
				{
					row.GetComponentInChildren<Text>().text = item.Label;

					if (!string.IsNullOrWhiteSpace(item.Tooltip))
					{
						Tooltip tooltip = row.AddComponent<Tooltip>();
						tooltip.Delay = 0.2f;
						tooltip.SetTooltip(item.Tooltip);
					}

					_blockConfigItems.Add(row);
				}
			}
		}

		LayoutRebuilder.MarkLayoutForRebuild(ConfigParent);
	}

	private void UpdateBlockConfigItems()
	{
		Debug.Assert(_selectedBlocks.Count > 0, nameof(_selectedBlocks) + ".Count > 0");

		UpdatingElements = true;

		List<JObject> configs = _selectedBlocks.Select(block => TypeUtils.ParseJson(block.Config)).ToList();

		int rowIndex = 0;
		foreach (
			IConfigComponent component in
			Builder.GetBlockObject(_selectedBlocks[0]).GetComponents<IConfigComponent>()
		)
		{
			string configKey = TypeUtils.GetClassKey(component.GetType());
			var currentConfigs =
				configs.Select(config => config.ContainsKey(configKey) ? (JObject) config[configKey] : null).ToList();

			foreach (ConfigItemBase item in component.GetConfigItems())
			{
				GameObject row = _blockConfigItems[rowIndex];

				switch (item)
				{
					case ToggleConfigItem toggleItem:
						row.GetComponent<Toggle>().isOn = GetToggleValue(currentConfigs, toggleItem);
						break;
					case IntSwitchSelectConfigItem intSwitchItem:
						row.GetComponent<SwitchSelect>().Value = GetIntSwitchValue(currentConfigs, intSwitchItem);
						break;
					case StringSwitchSelectConfigItem stringSwitchItem:
						row.GetComponent<SwitchSelect>().Value = GetStringSwitchValue(currentConfigs, stringSwitchItem);
						break;
					default:
						Debug.LogError($"Unhandled config item type {item.GetType()}");
						rowIndex--; // Counteract automatic increment
						break;
				}

				rowIndex++;
			}
		}

		UpdatingElements = false;
	}

	private static bool GetToggleValue(IEnumerable<JObject> currentConfigs, ToggleConfigItem toggleItem)
	{
		return currentConfigs.All(
			config => config != null
			          && config.ContainsKey(toggleItem.Key)
			          && config[toggleItem.Key].Value<bool>()
		);
	}

	private static int GetIntSwitchValue(IEnumerable<JObject> currentConfigs, IntSwitchSelectConfigItem intSwitchItem)
	{
		return currentConfigs
			.Select(
				config =>
					config != null && config.ContainsKey(intSwitchItem.Key)
						? config[intSwitchItem.Key].Value<int>()
						: -1
			)
			.FirstOrDefault(index => index >= 0);
	}

	private static int GetStringSwitchValue(
		IEnumerable<JObject> currentConfigs, StringSwitchSelectConfigItem stringSwitchItem
	)
	{
		return currentConfigs
			.Select(
				config =>
					config != null && config.ContainsKey(stringSwitchItem.Key)
						? stringSwitchItem.Deserialize(config[stringSwitchItem.Key].Value<string>())
						: -1
			)
			.FirstOrDefault(index => index >= 0);
	}

	private void SetBlockConfigs(Type componentType, string itemKey, JValue value)
	{
		if (UpdatingElements) return;

		foreach (VehicleBlueprint.BlockInstance selectedBlock in _selectedBlocks)
		{
			string componentKey = TypeUtils.GetClassKey(componentType);
			BlockConfigHelper.UpdateConfig(selectedBlock, new[] { componentKey, itemKey }, value);
			BlockConfigHelper.SyncConfig(selectedBlock, Builder.GetBlockObject(selectedBlock));
		}
	}

	private void UpdateSelectionIndicators()
	{
		HashSet<VehicleBlueprint.BlockInstance>
			newBlocks = new HashSet<VehicleBlueprint.BlockInstance>(_selectedBlocks);

		foreach (KeyValuePair<VehicleBlueprint.BlockInstance, GameObject> entry in _selectionIndicators)
		{
			entry.Value.SetActive(newBlocks.Remove(entry.Key));
		}

		foreach (VehicleBlueprint.BlockInstance blockInstance in newBlocks)
		{
			var blockSpec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);
			BoundsInt blockBounds = TransformUtils.TransformBounds(
				new BlockBounds(blockSpec.Construction.BoundsMin, blockSpec.Construction.BoundsMax).ToBoundsInt(),
				blockInstance.Position, blockInstance.Rotation
			);

			var indicator = Instantiate(SelectionIndicatorPrefab, IndicatorParent);
			indicator.transform.localPosition = blockBounds.center - new Vector3(0.5f, 0.5f, 0f);
			indicator.transform.localScale = blockBounds.size;
			indicator.SetActive(true);

			_selectionIndicators.Add(blockInstance, indicator);
		}
	}

	private void ClearSelectionIndicators()
	{
		foreach (GameObject indicator in _selectionIndicators.Values)
		{
			Destroy(indicator);
		}

		_selectionIndicators.Clear();
	}

	#endregion

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

	private void SetPidResponse(float response)
	{
		Blueprint.PidConfig.Response = response;
	}

	private void SetPidDerivativeTime(float derivativeTime)
	{
		Blueprint.PidConfig.DerivativeTime = derivativeTime;
	}

	private void SetPidIntegralTime(float integralTime)
	{
		Blueprint.PidConfig.IntegralTime = integralTime;
	}

	#endregion
}
}