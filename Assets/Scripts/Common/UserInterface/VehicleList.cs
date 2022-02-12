using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Common.UserInterface
{
[Serializable]
public class SelectVehicleEvent : UnityEvent<string>
{}

public class VehicleList : MonoBehaviour
{
	public const string VEHICLE_EXTENSION = ".vehicle";

	[Header("References")]
	public Transform ListParent;
	public bool CanShowStockVehicle;
	public Toggle ShowStockVehicleToggle;

	public GameObject VehicleRowPrefab;
	public GameObject EmptyDirectoryIndicator;

	[Header("Events")]
	public SelectVehicleEvent OnSelectVehicle;

	private static string _saveDir;
	public static string SaveDir => _saveDir ??= Path.Combine(Application.persistentDataPath, "Vehicles");

	private List<string> _vehiclePaths;
	private Dictionary<int, GameObject> _vehiclePanels;
	private int? _costLimit;
	private int _selectedIndex;

	private string _selectNameOnEnable;

	private void Awake()
	{
		_vehiclePaths = new List<string>();
		_vehiclePanels = new Dictionary<int, GameObject>();

		if (!Directory.Exists(SaveDir))
		{
			Directory.CreateDirectory(SaveDir);
		}
	}

	private void OnEnable()
	{
		_selectedIndex = -1;

		bool showStock = PlayerPrefs.GetInt(PropertyKeys.SHOW_STOCK_VEHICLES, 1) == 1;

		if (ShowStockVehicleToggle != null)
		{
			ShowStockVehicleToggle.isOn = showStock;
			ShowStockVehicleToggle.onValueChanged.AddListener(SetStockVehicleVisible);
		}

		if (CanShowStockVehicle && showStock)
		{
			foreach (string vehiclePath in StockVehicleDatabase.Instance.ListStockVehicles())
			{
				CreateVehicleRowButton(vehiclePath, true);
			}
		}

		string[] vehicles = Directory.GetFiles(SaveDir);

		foreach (string vehiclePath in vehicles)
		{
			if (Path.GetExtension(vehiclePath).Equals(VEHICLE_EXTENSION))
			{
				CreateVehicleRowButton(vehiclePath, false);
			}
		}

		EmptyDirectoryIndicator.SetActive(_vehiclePaths.Count <= 0);
	}

	private VehicleRowButton CreateVehicleRowButton(string vehiclePath, bool isStock)
	{
		VehicleBlueprint blueprint;

		try
		{
			string content = File.ReadAllText(vehiclePath);
			blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);
		}
		catch (ArgumentException exception)
		{
			Debug.LogError(exception);
			Debug.LogError($"When reading vehicle from {vehiclePath}");
			return null;
		}

		_vehiclePaths.Add(vehiclePath);
		GameObject go = Instantiate(VehicleRowPrefab, ListParent);

		var button = go.GetComponent<VehicleRowButton>();
		button.DisplayVehicle(blueprint, isStock);
		button.SetIndex(_vehiclePaths.Count - 1);
		button.SetCostColor(
			_costLimit.HasValue
				? _costLimit.Value < blueprint.CachedCost ? Color.red : Color.green
				: Color.white
		);

		_vehiclePanels.Add(_vehiclePaths.Count - 1, go);

		if (!isStock && blueprint.Name == _selectNameOnEnable)
		{
			button.SetSelected(true);
			_selectedIndex = _vehiclePaths.Count - 1;
		}

		return button;
	}

	private void OnDisable()
	{
		foreach (GameObject go in _vehiclePanels.Values)
		{
			Destroy(go);
		}

		if (ShowStockVehicleToggle != null)
		{
			ShowStockVehicleToggle.onValueChanged.RemoveListener(SetStockVehicleVisible);
		}

		_vehiclePanels.Clear();
		_vehiclePaths.Clear();
		_selectNameOnEnable = null;
	}

	private void SetStockVehicleVisible(bool showStock)
	{
		PlayerPrefs.SetInt(PropertyKeys.SHOW_STOCK_VEHICLES, showStock ? 1 : 0);

		string selectedPath = GetSelectedVehiclePath();

		foreach (GameObject go in _vehiclePanels.Values)
		{
			Destroy(go);
		}

		_vehiclePanels.Clear();
		_vehiclePanels.Clear();
		_selectNameOnEnable = null;

		if (CanShowStockVehicle && showStock)
		{
			foreach (string vehiclePath in StockVehicleDatabase.Instance.ListStockVehicles())
			{
				var button = CreateVehicleRowButton(vehiclePath, true);

				if (vehiclePath == selectedPath)
				{
					button.SetSelected(true);
					_selectedIndex = _vehiclePaths.Count - 1;
				}
			}
		}

		string[] vehicles = Directory.GetFiles(SaveDir);

		foreach (string vehiclePath in vehicles)
		{
			if (Path.GetExtension(vehiclePath).Equals(VEHICLE_EXTENSION))
			{
				var button = CreateVehicleRowButton(vehiclePath, false);

				if (vehiclePath == selectedPath)
				{
					button.SetSelected(true);
					_selectedIndex = _vehiclePaths.Count - 1;
				}
			}
		}

		EmptyDirectoryIndicator.SetActive(_vehiclePaths.Count <= 0);
	}

	public void SetCostLimit(int? costLimit)
	{
		_costLimit = costLimit;

		if (_vehiclePanels == null) return;

		foreach (var entry in _vehiclePanels)
		{
			string content = File.ReadAllText(_vehiclePaths[entry.Key]);
			VehicleBlueprint blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);

			entry.Value.GetComponent<VehicleRowButton>().SetCostColor(
				_costLimit.HasValue
					? _costLimit.Value < blueprint.CachedCost ? Color.red : Color.green
					: Color.white
			);
		}
	}

	public void SelectIndex(int index)
	{
		if (_selectedIndex >= 0 && _selectedIndex < _vehiclePanels.Count)
		{
			_vehiclePanels[_selectedIndex].GetComponent<VehicleRowButton>().SetSelected(false);
		}

		_selectedIndex = index;

		if (_selectedIndex >= 0)
		{
			_vehiclePanels[_selectedIndex].GetComponent<VehicleRowButton>().SetSelected(true);
		}

		OnSelectVehicle.Invoke(
			_selectedIndex >= 0 ? Path.GetFileNameWithoutExtension(_vehiclePaths[_selectedIndex]) : null
		);
	}

	public void SelectName(string vehicleName)
	{
		if (_vehiclePaths != null && _vehiclePaths.Count > 0)
		{
			int index = _vehiclePaths.FindIndex(path => Path.GetFileNameWithoutExtension(path) == vehicleName);
			SelectIndex(index);
		}
		else
		{
			_selectNameOnEnable = vehicleName;
		}
	}

	public string GetSelectedVehiclePath()
	{
		return _selectedIndex < 0 ? null : _vehiclePaths[_selectedIndex];
	}

	public string ToVehiclePath(string vehicleName)
	{
		return Path.Combine(SaveDir, vehicleName + VEHICLE_EXTENSION);
	}
}
}