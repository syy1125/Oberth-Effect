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

	[Header("References")]
	public VehicleDesigner Designer;

	public GameObject Backdrop;

	public GameObject BaseMenu;

	public NotificationDialog Notification;

	[Header("Events")]
	public UnityEvent OnMenuOpen;

	public UnityEvent OnMenuClosed;

	private bool _enabled;
	private Stack<GameObject> _modals;

	private void Awake()
	{
		_enabled = false;
		_modals = new Stack<GameObject>();
	}

	private void OnEnable()
	{
		MenuAction.action.Enable();
		MenuAction.action.performed += ToggleMenu;
	}

	private void OnDisable()
	{
		MenuAction.action.performed -= ToggleMenu;
		MenuAction.action.Disable();
	}

	private void ToggleMenu(InputAction.CallbackContext context)
	{
		if (!_enabled)
		{
			OpenMenu();
		}
		else
		{
			CloseTopModal();
		}
	}

	private void OpenMenu()
	{
		Backdrop.SetActive(true);
		OpenModal(BaseMenu);

		_enabled = true;
		OnMenuOpen.Invoke();
	}

	public void OpenModal(GameObject modal)
	{
		// Note: we can't use event system here because it doesn't work on disabled objects.
		foreach (MonoBehaviour behaviour in modal.GetComponents<MonoBehaviour>())
		{
			(behaviour as IModal)?.OpenModal();
		}

		if (_modals.Count > 0)
		{
			foreach (MonoBehaviour behaviour in _modals.Peek().GetComponents<MonoBehaviour>())
			{
				(behaviour as IModal)?.CloseModal();
			}
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

			_enabled = false;
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

		_enabled = false;
		OnMenuClosed.Invoke();
	}

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
		SceneManager.LoadScene("Scenes/Test Drive Room");
	}

	public void ToMainMenu()
	{
		SceneManager.LoadScene("Scenes/Main Menu");
	}
}
}