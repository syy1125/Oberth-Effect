using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VehicleLoadSave : MonoBehaviour, IModal
{
	private const string VEHICLE_EXTENSION = ".vehicle";

	[Header("Config")]
	public bool SaveMode;

	[Header("External References")]
	public VehicleDesigner Designer;

	public DesignerMenu Menu;

	[Header("Internal References")]
	public Transform ListParent;

	public GameObject VehicleRowPrefab;
	public GameObject EmptyDirectoryIndicator;

	public Button SaveLoadButton;
	public InputField FilenameInput;

	private string _saveDir;
	private List<string> _vehiclePaths;
	private int _selectedIndex;

	private void Awake()
	{
		_saveDir = Path.Combine(Application.persistentDataPath, "Vehicles");
		_vehiclePaths = new List<string>();

		if (!Directory.Exists(_saveDir))
		{
			Directory.CreateDirectory(_saveDir);
		}
	}

	private void OnEnable()
	{
		_selectedIndex = -1;
		SaveLoadButton.interactable = false;

		string[] vehicles = Directory.GetFiles(_saveDir);

		foreach (string vehiclePath in vehicles)
		{
			if (Path.GetExtension(vehiclePath).Equals(VEHICLE_EXTENSION))
			{
				_vehiclePaths.Add(vehiclePath);
				Instantiate(VehicleRowPrefab, ListParent);

				string content = File.ReadAllText(vehiclePath);
				var blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);
				VehicleRowPrefab.GetComponent<VehicleRowButton>().DisplayVehicle(blueprint);
			}
		}

		if (_vehiclePaths.Count > 0)
		{
			EmptyDirectoryIndicator.SetActive(false);
			EmptyDirectoryIndicator.transform.SetAsLastSibling();
		}
		else
		{
			EmptyDirectoryIndicator.SetActive(true);
		}

		if (SaveMode)
		{
			FilenameInput.onEndEdit.AddListener(HandleFilenameChange);
			SaveLoadButton.onClick.AddListener(SaveVehicle);
		}
		else
		{
			SaveLoadButton.onClick.AddListener(LoadVehicle);
		}
	}

	private void OnDisable()
	{
		foreach (Transform row in ListParent)
		{
			if (row.gameObject != EmptyDirectoryIndicator)
			{
				Destroy(row.gameObject);
			}
		}

		_vehiclePaths.Clear();

		if (SaveMode)
		{
			FilenameInput.onEndEdit.RemoveListener(HandleFilenameChange);
			SaveLoadButton.onClick.RemoveListener(SaveVehicle);
		}
		else
		{
			SaveLoadButton.onClick.RemoveListener(LoadVehicle);
		}
	}

	private void HandleFilenameChange(string filename)
	{
		SaveLoadButton.interactable = !string.IsNullOrWhiteSpace(filename);
	}

	public void SelectIndex(int index)
	{
		if (_selectedIndex >= 0)
		{
			ListParent.GetChild(_selectedIndex).GetComponent<VehicleRowButton>().SetSelected(false);
		}

		_selectedIndex = index;

		if (_selectedIndex >= 0)
		{
			ListParent.GetChild(_selectedIndex).GetComponent<VehicleRowButton>().SetSelected(true);
		}

		if (SaveMode)
		{
			FilenameInput.text = Path.GetFileNameWithoutExtension(_vehiclePaths[_selectedIndex]);
		}
		else
		{
			SaveLoadButton.interactable = _selectedIndex >= 0;
		}
	}

	private void SaveVehicle()
	{
		if (string.IsNullOrWhiteSpace(FilenameInput.text))
		{
			Debug.LogError("VehicleLoadSave.SaveVehicle called when filename is empty!");
			return;
		}

		Designer.RenameVehicle(FilenameInput.text);
		VehicleBlueprint blueprint = Designer.SaveVehicle();
		string content = JsonUtility.ToJson(blueprint);
		string filePath = Path.Combine(_saveDir, FilenameInput.text + VEHICLE_EXTENSION);
		File.WriteAllText(filePath, content);
	}

	private void LoadVehicle()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _vehiclePaths.Count)
		{
			Debug.LogError(
				$"VehicleLoadSave.LoadVehicle called when selected index is out of bounds {_selectedIndex}/{_vehiclePaths.Count}"
			);
		}

		string content = File.ReadAllText(_vehiclePaths[_selectedIndex]);
		var blueprint = JsonUtility.FromJson<VehicleBlueprint>(content);
		Designer.LoadVehicle(blueprint);

		Menu.CloseAllModals();
	}

	public void OpenModal()
	{
		gameObject.SetActive(true);
	}

	public void CloseModal()
	{
		gameObject.SetActive(false);
	}
}