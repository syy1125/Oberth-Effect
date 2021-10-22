using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.Designer.Menu
{
public class DesignerMenu : MonoBehaviour
{
	[Header("Input")]
	public InputActionReference MenuAction;
	public InputActionReference HelpAction;

	[Header("References")]
	public VehicleDesigner Designer;
	public GameObject Backdrop;

	public GameObject BaseMenu;
	public NotificationDialog Notification;
	public GameObject HelpScreen;

	public SceneReference TestDriveScene;
	public SceneReference MainMenuScene;

	[Header("Events")]
	public UnityEvent OnMenuOpen;
	public UnityEvent OnMenuClosed;

	private Stack<GameObject> _modals;

	private void Awake()
	{
		_modals = new Stack<GameObject>();
	}

	private void OnEnable()
	{
		MenuAction.action.Enable();
		HelpAction.action.Enable();
		MenuAction.action.performed += ToggleMenu;
		HelpAction.action.performed += ToggleHelp;
	}

	private void OnDisable()
	{
		MenuAction.action.performed -= ToggleMenu;
		HelpAction.action.performed -= ToggleHelp;
		MenuAction.action.Disable();
		HelpAction.action.Disable();
	}

	private void ToggleMenu(InputAction.CallbackContext context)
	{
		if (_modals.Count > 0)
		{
			CloseTopModal();
		}
		else
		{
			OpenModal(BaseMenu);
		}
	}

	private void ToggleHelp(InputAction.CallbackContext context)
	{
		if (_modals.Count > 0 && _modals.Peek() == HelpScreen)
		{
			CloseTopModal();
		}
		else
		{
			OpenModal(HelpScreen);
		}
	}

	#region Generic Modal Management

	public void OpenModal(GameObject modal)
	{
		if (_modals.Count > 0)
		{
			foreach (MonoBehaviour behaviour in _modals.Peek().GetComponents<MonoBehaviour>())
			{
				(behaviour as IModal)?.CloseModal();
			}
		}
		else
		{
			Backdrop.SetActive(true);
			OnMenuOpen.Invoke();
		}

		// Note: we can't use event system here because it doesn't work on disabled objects.
		foreach (MonoBehaviour behaviour in modal.GetComponents<MonoBehaviour>())
		{
			(behaviour as IModal)?.OpenModal();
		}

		_modals.Push(modal);
	}

	public void CloseTopModal()
	{
		GameObject modal = _modals.Pop();

		foreach (MonoBehaviour behaviour in modal.GetComponents<MonoBehaviour>())
		{
			(behaviour as IModal)?.CloseModal();
		}

		if (_modals.Count > 0)
		{
			foreach (MonoBehaviour behaviour in _modals.Peek().GetComponents<MonoBehaviour>())
			{
				(behaviour as IModal)?.OpenModal();
			}
		}
		else
		{
			Backdrop.SetActive(false);
			OnMenuClosed.Invoke();
		}
	}

	public void CloseAllModals()
	{
		while (_modals.Count > 0)
		{
			foreach (MonoBehaviour behaviour in _modals.Pop().GetComponents<MonoBehaviour>())
			{
				(behaviour as IModal)?.CloseModal();
			}
		}

		Backdrop.SetActive(false);
		OnMenuClosed.Invoke();
	}

	#endregion

	public void OpenSaveVehicle(GameObject saveModal)
	{
		List<string> errors = Designer.GetVehicleErrors();

		if (errors.Count > 0)
		{
			Notification.SetContent("Invalid design:\n" + string.Join("\n", errors));
			OpenModal(Notification.gameObject);
		}
		else
		{
			OpenModal(saveModal);
		}
	}
	
	public void ToTestDrive()
	{
		VehicleSelection.SerializedVehicle = Designer.ExportVehicle();
		SceneManager.LoadScene(TestDriveScene);
	}

	public void ToMainMenu()
	{
		SceneManager.LoadScene(MainMenuScene);
	}
}
}