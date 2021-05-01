using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.Designer;
using Syy1125.OberthEffect.Vehicle;
using UnityEngine;
using UnityEngine.Events;

namespace Syy1125.OberthEffect
{
[Serializable]
public class SelectVehicleEvent : UnityEvent<string>
{}

public class VehicleList : MonoBehaviour
{
	private const string VEHICLE_EXTENSION = ".vehicle";

	[Header("References")]
	public Transform ListParent;

	public GameObject VehicleRowPrefab;
	public GameObject EmptyDirectoryIndicator;

	[Header("Events")]
	public SelectVehicleEvent OnSelectVehicle;

	private string _saveDir;
	private List<string> _vehiclePaths;
	private Dictionary<int, GameObject> _vehiclePanels;
	private int _selectedIndex;

	private void Awake()
	{
		_saveDir = Path.Combine(Application.persistentDataPath, "Vehicles");
		_vehiclePaths = new List<string>();
		_vehiclePanels = new Dictionary<int, GameObject>();

		if (!Directory.Exists(_saveDir))
		{
			Directory.CreateDirectory(_saveDir);
		}
	}

	private void OnEnable()
	{
		_selectedIndex = -1;

		string[] vehicles = Directory.GetFiles(_saveDir);

		foreach (string vehiclePath in vehicles)
		{
			if (Path.GetExtension(vehiclePath).Equals(VEHICLE_EXTENSION))
			{
				_vehiclePaths.Add(vehiclePath);
				GameObject go = Instantiate(VehicleRowPrefab, ListParent);

				string content = File.ReadAllText(vehiclePath);
				var blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);
				go.GetComponent<VehicleRowButton>().DisplayVehicle(blueprint);
				go.GetComponent<VehicleRowButton>().SetIndex(_vehiclePaths.Count - 1);

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
		return Path.Combine(_saveDir, vehicleName + VEHICLE_EXTENSION);
	}
}
}