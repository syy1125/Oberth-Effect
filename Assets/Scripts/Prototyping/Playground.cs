using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	public InputActionAsset Controls;
	public InputActionReference Move;

	private void Start()
	{
		// InputSystem.settings.SetInternalFeatureFlag("DISABLE_SHORTCUT_SUPPORT", true);
		// Controls.FindActionMap("Player").Enable();
	}

	private void Update()
	{
		Debug.Log(Move.action.ReadValue<Vector2>());
	}
}
}