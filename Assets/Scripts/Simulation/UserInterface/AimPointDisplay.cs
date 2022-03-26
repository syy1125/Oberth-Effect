using System;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(CanvasGroup))]
public class AimPointDisplay : MonoBehaviour
{
	public InputActionReference LookAction;
	private RectTransform _parentTransform;
	private bool _visible;

	private void Awake()
	{
		_parentTransform = transform.parent.GetComponent<RectTransform>();
	}

	private void OnEnable()
	{
		UpdateVisibility();
		PlayerControlConfig.Instance.InvertAimChanged.AddListener(UpdateVisibility);
	}

	private void OnDisable()
	{
		if (PlayerControlConfig.Instance != null)
		{
			PlayerControlConfig.Instance.InvertAimChanged.RemoveListener(UpdateVisibility);
		}
	}

	private void UpdateVisibility()
	{
		_visible = PlayerControlConfig.Instance.InvertAim;
		GetComponent<CanvasGroup>().alpha = _visible ? 1 : 0;
	}

	private void Update()
	{
		if (!_visible) return;
		Vector2 screenPosition = new Vector2(Screen.width, Screen.height) - LookAction.action.ReadValue<Vector2>();

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
			    _parentTransform, screenPosition, null, out Vector2 localPoint
		    ))
		{
			transform.localPosition = localPoint;
		}
		else
		{
			Debug.LogError("Failed to calculate local position for crosshairs!");
		}
	}
}
}