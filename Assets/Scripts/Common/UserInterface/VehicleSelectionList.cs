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
public class VehicleListItem
{
	public string FilePath;
	public VehicleBlueprint Blueprint;
	public GameObject Panel;
}

[Serializable]
public class SelectVehicleEvent : UnityEvent<string, VehicleBlueprint>
{}

public class VehicleSelectionList : MonoBehaviour
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

	private List<VehicleListItem> _vehicles;
	private int? _costLimit;
	private int _selectedIndex;

	private string _selectNameOnEnable;

	private void Awake()
	{
		_vehicles = new List<VehicleListItem>();

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

		EmptyDirectoryIndicator.SetActive(_vehicles.Count <= 0);
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

		GameObject row = Instantiate(VehicleRowPrefab, ListParent);

		var button = row.GetComponent<VehicleRowButton>();
		button.DisplayVehicle(blueprint, isStock);
		button.SetIndex(_vehicles.Count);
		button.SetCostColor(
			_costLimit.HasValue
				? _costLimit.Value < blueprint.CachedCost ? Color.red : Color.green
				: Color.white
		);

		_vehicles.Add(
			new VehicleListItem
			{
				FilePath = vehiclePath,
				Blueprint = blueprint,
				Panel = row
			}
		);

		if (!isStock && blueprint.Name == _selectNameOnEnable)
		{
			button.SetSelected(true);
			_selectedIndex = _vehicles.Count - 1;
		}

		return button;
	}

	private void OnDisable()
	{
		foreach (VehicleListItem item in _vehicles)
		{
			Destroy(item.Panel);
		}

		if (ShowStockVehicleToggle != null)
		{
			ShowStockVehicleToggle.onValueChanged.RemoveListener(SetStockVehicleVisible);
		}

		_vehicles.Clear();
		_selectNameOnEnable = null;
	}

	private void SetStockVehicleVisible(bool showStock)
	{
		PlayerPrefs.SetInt(PropertyKeys.SHOW_STOCK_VEHICLES, showStock ? 1 : 0);

		string selectedPath = GetSelectedVehiclePath();

		foreach (VehicleListItem item in _vehicles)
		{
			Destroy(item.Panel);
		}

		_vehicles.Clear();
		_selectNameOnEnable = null;

		if (CanShowStockVehicle && showStock)
		{
			foreach (string vehiclePath in StockVehicleDatabase.Instance.ListStockVehicles())
			{
				var button = CreateVehicleRowButton(vehiclePath, true);

				if (vehiclePath == selectedPath)
				{
					button.SetSelected(true);
					_selectedIndex = _vehicles.Count - 1;
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
					_selectedIndex = _vehicles.Count - 1;
				}
			}
		}

		EmptyDirectoryIndicator.SetActive(_vehicles.Count <= 0);
	}

	public void SetCostLimit(int? costLimit)
	{
		_costLimit = costLimit;

		if (_vehicles == null || _vehicles.Count == 0) return;

		foreach (var entry in _vehicles)
		{
			string content = File.ReadAllText(entry.FilePath);
			VehicleBlueprint blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);

			entry.Panel.GetComponent<VehicleRowButton>().SetCostColor(
				_costLimit.HasValue
					? _costLimit.Value < blueprint.CachedCost ? Color.red : Color.green
					: Color.white
			);
		}
	}

	public void SelectIndex(int index)
	{
		if (_selectedIndex >= 0 && _selectedIndex < _vehicles.Count)
		{
			_vehicles[_selectedIndex].Panel.GetComponent<VehicleRowButton>().SetSelected(false);
		}

		_selectedIndex = index;

		if (_selectedIndex >= 0)
		{
			_vehicles[_selectedIndex].Panel.GetComponent<VehicleRowButton>().SetSelected(true);
		}

		if (_selectedIndex >= 0)
		{
			OnSelectVehicle.Invoke(_vehicles[_selectedIndex].FilePath, _vehicles[_selectedIndex].Blueprint);
		}
		else
		{
			OnSelectVehicle.Invoke(null, null);
		}
	}

	public void SelectName(string vehicleName)
	{
		if (_vehicles != null && _vehicles.Count > 0)
		{
			int index = _vehicles.FindIndex(item => item.Blueprint.Name == vehicleName);
			SelectIndex(index);
		}
		else
		{
			_selectNameOnEnable = vehicleName;
		}
	}

	public string GetSelectedVehiclePath()
	{
		return _selectedIndex < 0 ? null : _vehicles[_selectedIndex].FilePath;
	}

	public VehicleBlueprint GetSelectedVehicleBlueprint()
	{
		return _selectedIndex < 0 ? null : _vehicles[_selectedIndex].Blueprint;
	}

	public static string GetVehicleSavePath(string vehicleName)
	{
		return Path.Combine(SaveDir, vehicleName + VEHICLE_EXTENSION);
	}
}
}