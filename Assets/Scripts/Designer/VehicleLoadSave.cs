using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VehicleLoadSave : MonoBehaviour, IModal
{
	private const string VEHICLE_EXTENSION = ".vehicle";

	[Header("Config")]
	public bool SaveMode;

	[Header("References")]
	public Transform ListParent;

	public GameObject VehicleRowPrefab;
	public GameObject EmptyDirectoryIndicator;

	public Button BackButton;
	public Button SaveLoadButton;
	public InputField FileNameInput;

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
				VehicleRowPrefab.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(vehiclePath);
			}
		}

		if (_vehiclePaths.Count > 0)
		{
			EmptyDirectoryIndicator.SetActive(false);
		}
		else
		{
			EmptyDirectoryIndicator.SetActive(true);
			EmptyDirectoryIndicator.transform.SetAsLastSibling();
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
			FileNameInput.text = Path.GetFileNameWithoutExtension(_vehiclePaths[_selectedIndex]);
		}
		else
		{
			SaveLoadButton.interactable = _selectedIndex >= 0;
		}
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