using System.IO;
using Syy1125.OberthEffect.Vehicle;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer
{
public class VehicleLoadSave : MonoBehaviour, IModal
{
	[Header("Config")]
	public bool SaveMode;

	[Header("External References")]
	public VehicleDesigner Designer;

	public DesignerMenu Menu;

	[Header("Internal References")]
	public VehicleList VehicleList;

	public Button SaveLoadButton;

	[FormerlySerializedAs("FilenameInput")]
	public InputField FileNameInput;

	private string _vehiclePath;

	private void OnEnable()
	{
		SaveLoadButton.interactable = false;
		VehicleList.OnSelectVehicle.AddListener(HandleSelectVehicle);

		if (SaveMode)
		{
			FileNameInput.onValueChanged.AddListener(HandleFileNameChange);
			SaveLoadButton.onClick.AddListener(SaveVehicle);
		}
		else
		{
			SaveLoadButton.onClick.AddListener(LoadVehicle);
		}
	}

	private void OnDisable()
	{
		VehicleList.OnSelectVehicle.RemoveListener(HandleSelectVehicle);

		if (SaveMode)
		{
			FileNameInput.onEndEdit.RemoveListener(HandleFileNameChange);
			SaveLoadButton.onClick.RemoveListener(SaveVehicle);
		}
		else
		{
			SaveLoadButton.onClick.RemoveListener(LoadVehicle);
		}
	}

	private void HandleFileNameChange(string filename)
	{
		SaveLoadButton.interactable = !string.IsNullOrWhiteSpace(filename);
	}

	private void HandleSelectVehicle(string vehicleName)
	{
		if (SaveMode)
		{
			if (vehicleName != null)
			{
				FileNameInput.text = vehicleName;
			}
		}
		else
		{
			SaveLoadButton.interactable = vehicleName != null;
		}
	}

	private void SaveVehicle()
	{
		if (string.IsNullOrWhiteSpace(FileNameInput.text))
		{
			Debug.LogError("VehicleLoadSave.SaveVehicle called when filename is empty!");
			return;
		}

		Debug.Log($"Saving {FileNameInput.text}");

		Designer.RenameVehicle(FileNameInput.text);
		VehicleBlueprint blueprint = Designer.SaveVehicle();
		string content = JsonUtility.ToJson(blueprint);
		string filePath = VehicleList.ToVehiclePath(FileNameInput.text);
		File.WriteAllText(filePath, content);
	}

	private void LoadVehicle()
	{
		string vehiclePath = VehicleList.GetSelectedVehiclePath();

		if (vehiclePath == null)
		{
			Debug.LogError(
				$"VehicleLoadSave.LoadVehicle called when vehiclePath is null"
			);
			return;
		}

		Debug.Log($"Loading vehicle from {vehiclePath}");

		string content = File.ReadAllText(vehiclePath);
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
}