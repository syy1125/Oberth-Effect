using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.UserInterface;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
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
	public InputActionReference MultiSelectAction;

	[Header("References")]
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;
	public DesignerAreaMask AreaMask;
	public GameObject SelectionIndicatorPrefab;
	public Transform IndicatorParent;
	public Text StatusText;
	public RectTransform ConfigParent;

	[Header("Vehicle Config")]
	public SwitchSelect ControlModeSelect;
	public Toggle CustomColorToggle;
	public ColorPicker PrimaryColorPicker;
	public ColorPicker SecondaryColorPicker;
	public ColorPicker TertiaryColorPicker;

	[Header("Block Config")]
	public GameObject TogglePrefab;
	public GameObject SwitchSelectPrefab;

	private VehicleBlueprint Blueprint => Designer.Blueprint;

	private ColorContext _context;

	private VehicleBlueprint.BlockInstance _prevSelectBlock;
	private bool _additive;
	private List<VehicleBlueprint.BlockInstance> _selectedBlocks;
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _selectionIndicators;

	private string _configBlockId;
	private List<GameObject> _blockConfigItems;
	private bool _updatingElements;

	public bool HasSelectedBlock => _selectedBlocks.Count > 0;

	#region Initialization

	private void Awake()
	{
		_context = GetComponentInParent<ColorContext>();
		_selectedBlocks = new List<VehicleBlueprint.BlockInstance>();
		_selectionIndicators = new Dictionary<VehicleBlueprint.BlockInstance, GameObject>();
		_blockConfigItems = new List<GameObject>();
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
		// TODO clean up config items
		ControlModeSelect.Value = (int) Blueprint.DefaultControlMode;

		ColorScheme colorScheme = ColorScheme.FromBlueprint(Designer.Blueprint);

		CustomColorToggle.isOn = Blueprint.UseCustomColors;

		PrimaryColorPicker.InitColor(colorScheme.PrimaryColor);
		SecondaryColorPicker.InitColor(colorScheme.SecondaryColor);
		TertiaryColorPicker.InitColor(colorScheme.TertiaryColor);

		_context.SetColorScheme(colorScheme);
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
		ClearBlockConfigItems();
		_configBlockId = null;

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

		string blockId = _selectedBlocks[0].BlockId;
		for (int i = 1; i < _selectedBlocks.Count; i++)
		{
			if (_selectedBlocks[i].BlockId != blockId)
			{
				StatusText.text = "Block type mismatch.";
				ClearBlockConfigItems();
				_configBlockId = null;
				return;
			}
		}

		// From this point on, we known that all blocks are of the same time (same block id).
		BlockSpec blockSpec = BlockDatabase.Instance.GetSpecInstance(blockId).Spec;

		StatusText.text = string.Join(
			"\n",
			_selectedBlocks.Count > 1
				? $"Configuring {_selectedBlocks.Count} blocks."
				: $"Configuring {blockSpec.Info.FullName} at {_selectedBlocks[0].Position}.",
			"Press 'Q' to show vehicle config"
		);

		if (blockId != _configBlockId)
		{
			ClearBlockConfigItems();
			CreateBlockConfigItems();
			_configBlockId = blockId;
		}
		else
		{
			UpdateBlockConfigItems();
		}
	}

	#region Config Display

	private void SetVehicleConfigEnabled(bool configEnabled)
	{
		ControlModeSelect.gameObject.SetActive(configEnabled);
		CustomColorToggle.gameObject.SetActive(configEnabled);
		PrimaryColorPicker.gameObject.SetActive(configEnabled);
		SecondaryColorPicker.gameObject.SetActive(configEnabled);
		TertiaryColorPicker.gameObject.SetActive(configEnabled);
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

		List<JObject> configs = _selectedBlocks.Select(block => ConfigUtils.ParseConfig(block.Config)).ToList();

		foreach (
			IConfigComponent component in
			Builder.GetBlockObject(_selectedBlocks[0]).GetComponents<IConfigComponent>()
		)
		{
			Type componentType = component.GetType();
			string configKey = ConfigUtils.GetConfigKey(componentType);
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

		_updatingElements = true;

		List<JObject> configs = _selectedBlocks.Select(block => ConfigUtils.ParseConfig(block.Config)).ToList();

		int rowIndex = 0;
		foreach (
			IConfigComponent component in
			Builder.GetBlockObject(_selectedBlocks[0]).GetComponents<IConfigComponent>()
		)
		{
			string configKey = ConfigUtils.GetConfigKey(component.GetType());
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

		_updatingElements = false;
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
		if (_updatingElements) return;

		foreach (VehicleBlueprint.BlockInstance selectedBlock in _selectedBlocks)
		{
			string componentKey = ConfigUtils.GetConfigKey(componentType);
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
			var blockSpec = BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec;
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

	#endregion
}
}