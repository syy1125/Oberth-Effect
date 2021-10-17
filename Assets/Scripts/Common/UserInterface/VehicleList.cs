using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

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

		string[] vehicles = Directory.GetFiles(SaveDir);

		foreach (string vehiclePath in vehicles)
		{
			if (Path.GetExtension(vehiclePath).Equals(VEHICLE_EXTENSION))
			{
				_vehiclePaths.Add(vehiclePath);
				GameObject go = Instantiate(VehicleRowPrefab, ListParent);

				string content = File.ReadAllText(vehiclePath);
				var blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);

				var button = go.GetComponent<VehicleRowButton>();
				button.DisplayVehicle(blueprint);
				button.SetIndex(_vehiclePaths.Count - 1);
				button.SetCostColor(
					_costLimit.HasValue
						? _costLimit.Value < blueprint.CachedCost ? Color.red : Color.green
						: Color.white
				);

				_vehiclePanels.Add(_vehiclePaths.Count - 1, go);
			}
		}

		EmptyDirectoryIndicator.SetActive(_vehiclePaths.Count <= 0);
	}

	private void OnDisable()
	{
		foreach (GameObject go in _vehiclePanels.Values)
		{
			Destroy(go);
		}

		_vehiclePanels.Clear();
		_vehiclePaths.Clear();
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
		if (_selectedIndex >= 0)
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