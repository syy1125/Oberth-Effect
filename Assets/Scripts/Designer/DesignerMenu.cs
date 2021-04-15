using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DesignerMenu : MonoBehaviour
{
	[Header("Input")]
	public InputActionReference MenuAction;

	[Header("References")]
	public GameObject Backdrop;

	public GameObject BaseMenu;

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
			Backdrop.SetActive(true);
			BaseMenu.SetActive(true);

			_enabled = true;
		}
		else if (_modals.Count > 0)
		{
			CloseTopModal();
		}
		else
		{
			Backdrop.SetActive(false);
			BaseMenu.SetActive(false);

			_enabled = false;
		}
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

	public void ToMainMenu()
	{
		SceneManager.LoadScene("Scenes/Main Menu");
	}
}