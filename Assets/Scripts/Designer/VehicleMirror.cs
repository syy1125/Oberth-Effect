using System.Collections;
using Syy1125.OberthEffect.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Designer
{
public class VehicleMirror : MonoBehaviour
{
	private static readonly float[] intervals = { 0.5f, 0.2f, 0.1f };

	[Header("References")]
	public VehicleDesigner Designer;
	public LineRenderer MirrorIndicator;

	[Header("Input")]
	public InputActionReference ToggleMirrorAction;
	public InputActionReference MoveMirrorAction;

	private VehicleBlueprint Blueprint => Designer.Blueprint;
	public bool UseMirror => Blueprint.UseMirror;
	public int MirrorPosition => Blueprint.MirrorPosition;

	private Camera _mainCamera;
	private int _moveDirection;
	private Coroutine _moveCoroutine;

	private void Awake()
	{
		_mainCamera = Camera.main;
	}

	private void OnEnable()
	{
		ToggleMirrorAction.action.performed += HandleToggleMirror;
	}

	private void OnDisable()
	{
		ToggleMirrorAction.action.performed -= HandleToggleMirror;
	}

	private void Update()
	{
		float input = MoveMirrorAction.action.ReadValue<float>();
		int direction = input > Mathf.Epsilon ? 1 : input < -Mathf.Epsilon ? -1 : 0;

		if (direction != _moveDirection)
		{
			if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);

			if (direction != 0)
			{
				_moveCoroutine = StartCoroutine(MoveMirror(direction));
			}

			_moveDirection = direction;
		}
	}

	private void LateUpdate()
	{
		Vector2 bottom = transform.InverseTransformPoint(_mainCamera.ViewportToWorldPoint(Vector3.zero));
		Vector2 top = transform.InverseTransformPoint(_mainCamera.ViewportToWorldPoint(Vector3.up));
		MirrorIndicator.enabled = UseMirror;
		MirrorIndicator.SetPosition(0, new Vector3(Blueprint.MirrorPosition / 2f, bottom.y, 0f));
		MirrorIndicator.SetPosition(1, new Vector3(Blueprint.MirrorPosition / 2f, top.y, 0f));
	}

	private void HandleToggleMirror(InputAction.CallbackContext context)
	{
		Blueprint.UseMirror = !Blueprint.UseMirror;
	}

	private IEnumerator MoveMirror(int direction)
	{
		for (int i = 0;; i = Mathf.Min(i + 1, intervals.Length - 1))
		{
			Blueprint.MirrorPosition += direction;
			yield return new WaitForSeconds(intervals[i]);
		}
	}

	public void ReloadVehicle()
	{
		// TODO reposition mirror indicator
	}
}
}