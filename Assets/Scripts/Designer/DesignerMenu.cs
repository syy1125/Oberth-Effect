using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerMenu : MonoBehaviour
{
	[Header("Input")]
	public InputActionReference MenuAction;

	[Header("References")]
	public GameObject Backdrop;

	public GameObject BaseMenu;
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
		else if (_modals.Count > 0)
		{
			CloseTopModal();
		}
		else
		{
			CloseMenu();
		}
	}

	private void OpenMenu()
	{
		Backdrop.SetActive(true);
		BaseMenu.SetActive(true);

		_enabled = true;
		OnMenuOpen.Invoke();
	}

	private void CloseMenu()
	{
		Backdrop.SetActive(false);
		BaseMenu.SetActive(false);

		_enabled = false;
		OnMenuClosed.Invoke();
	}

	public void OpenModal(GameObject modal)
	{
		foreach (MonoBehaviour behaviour in modal.GetComponents<MonoBehaviour>())
		{
			(behaviour as IModal)?.OpenModal();
		}

		_modals.Push(modal);

		BaseMenu.SetActive(false);
	}

	public void CloseTopModal()
	{
		GameObject modal = _modals.Pop();

		foreach (MonoBehaviour behaviour in modal.GetComponents<MonoBehaviour>())
		{
			(behaviour as IModal)?.CloseModal();
		}

		if (_modals.Count == 0)
		{
			BaseMenu.SetActive(true);
		}
	}

	public void CloseAllModals()
	{
		while (_modals.Count > 0)
		{
			CloseTopModal();
		}

		CloseMenu();
	}

	public void ToMainMenu()
	{
		SceneManager.LoadScene("Scenes/Main Menu");
	}
}
}