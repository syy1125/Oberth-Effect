﻿using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class QuickHelpScreen : MonoBehaviour
{
	public InputActionReference HelpAction;
	public GameObject HelpPage;

	private void OnEnable()
	{
		HelpAction.action.Enable();
		HelpAction.action.performed += HandleHelp;
	}

	private void OnDisable()
	{
		HelpAction.action.performed -= HandleHelp;
		HelpAction.action.Disable();
	}

	private void HandleHelp(InputAction.CallbackContext context)
	{
		HelpPage.SetActive(!HelpPage.activeSelf);
	}
}
}