using System.Collections;
using System.IO;
using Syy1125.OberthEffect.Common.UserInterface;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class VehicleLoadSave : MonoBehaviour, IModal
{
	[Header("Config")]
	public bool SaveMode;

	[Header("External References")]
	public VehicleDesigner Designer;

	public DesignerMenu Menu;

	public NotificationDialog Notification;

	[Header("Internal References")]
	public VehicleList VehicleList;

	public Button SaveLoadButton;

	[FormerlySerializedAs("FilenameInput")]
	public InputField FileNameInput;

	private string _vehiclePath;

	private void OnEnable()
	{
		VehicleList.OnSelectVehicle.AddListener(HandleSelectVehicle);

		if (SaveMode)
		{
			string vehicleName = Designer.Blueprint.Name;

			if (!string.IsNullOrWhiteSpace(vehicleName))
			{
				FileNameInput.text = vehicleName;
				VehicleList.SelectName(vehicleName);
				FileNameInput.text = vehicleName;
				FileNameInput.interactable = true;
			}
			else
			{
				FileNameInput.interactable = false;
			}

			FileNameInput.onValueChanged.AddListener(HandleFileNameChange);
			SaveLoadButton.onClick.AddListener(SaveVehicle);
		}
		else
		{
			SaveLoadButton.interactable = false;
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

		Designer.Builder.RenameVehicle(FileNameInput.text);
		string content = Designer.ExportVehicle();
		string filePath = VehicleList.ToVehiclePath(FileNameInput.text);
		File.WriteAllText(filePath, content);

		Notification.SetContent($"Vehicle {FileNameInput.text} saved!");
		Menu.CloseTopModal();
		Menu.OpenModal(Notification.gameObject);
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
		Designer.ImportVehicle(content);

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