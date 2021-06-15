using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleThrusterControl : MonoBehaviour, IResourceConsumer
{
	[Header("Input")]
	public InputActionReference MoveAction;
	public InputActionReference StrafeAction;
	public InputActionReference InertiaDampenerAction;

	[Header("PID")]
	public float RotateResponse;
	public float RotateDerivativeTime;
	public float RotateIntegralTime;

	[Header("Config")]
	public float InertiaDampenerStrength;

	[Header("UI")]
	public Text InertiaDampenerStatusIndicator;

	private bool _isMine;
	private bool _inertiaDampenerActive;
	private List<ThrusterResponse> _thrusterResponses;
	private Dictionary<VehicleResource, float> _resourceRequests;
	private float _resourceSatisfaction;

	private Camera _mainCamera;
	private Rigidbody2D _body;

	private LinkedList<float> _angleHistory;
	private float _integral;

	public float ForwardBackCommand { get; private set; }
	public float StrafeCommand { get; private set; }
	public float RotateCommand { get; private set; }

	private void Awake()
	{
		_thrusterResponses = new List<ThrusterResponse>();
		_resourceRequests = new Dictionary<VehicleResource, float>();

		_mainCamera = Camera.main;
		_body = GetComponent<Rigidbody2D>();
		_angleHistory = new LinkedList<float>();

		var photonView = GetComponent<PhotonView>();
		// Vehicle is mine if we're in singleplayer or if the photon view is mine.
		_isMine = photonView == null || photonView.IsMine;
	}

	private void OnEnable()
	{
		MoveAction.action.Enable();
		StrafeAction.action.Enable();

		InertiaDampenerAction.action.Enable();
		InertiaDampenerAction.action.performed += ToggleInertiaDampener;
	}

	private void Start()
	{
		_inertiaDampenerActive = false;
		UpdateUserInterface();
	}

	private void OnDisable()
	{
		MoveAction.action.Disable();
		StrafeAction.action.Disable();

		InertiaDampenerAction.action.performed -= ToggleInertiaDampener;
		InertiaDampenerAction.action.Disable();
	}

	#region Update

	private void FixedUpdate()
	{
		if (!_isMine) return;

		UpdateMouseModeCommands();

		if (_inertiaDampenerActive)
		{
			ApplyInertiaDampener();
		}

		ClampCommands();

		SendThrustCommands();
	}

	private void UpdateMouseModeCommands()
	{
		var move = MoveAction.action.ReadValue<Vector2>();
		var strafe = StrafeAction.action.ReadValue<float>();

		ForwardBackCommand = move.y;
		StrafeCommand = move.x + strafe;

		Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		Vector2 vehiclePosition = _body.worldCenterOfMass;
		float angle = Vector2.SignedAngle(mousePosition - vehiclePosition, transform.up);

		float derivative = _angleHistory.Count > 0 ? (angle - _angleHistory.Last.Value) / Time.fixedDeltaTime : 0f;
		derivative = (derivative + 540f) % 360f - 180f;

		_integral += angle;
		_angleHistory.AddLast(angle);
		while (_angleHistory.Count > 0 && _angleHistory.Count > RotateIntegralTime / Time.fixedDeltaTime)
		{
			_integral -= _angleHistory.First.Value;
			_angleHistory.RemoveFirst();
		}

		float timeScaledIntegral = _integral * Time.fixedDeltaTime;

		RotateCommand = RotateResponse
		                * (
			                angle
			                + derivative * RotateDerivativeTime
			                + (Mathf.Abs(RotateIntegralTime) < Mathf.Epsilon
				                ? 0f
				                : timeScaledIntegral / RotateIntegralTime)
		                )
		                * Mathf.Deg2Rad;
	}

	private void ApplyInertiaDampener()
	{
		Vector2 localVelocity = transform.InverseTransformVector(_body.velocity);

		if (Mathf.Approximately(ForwardBackCommand, 0))
		{
			ForwardBackCommand = -localVelocity.y * InertiaDampenerStrength;
		}

		if (Mathf.Approximately(StrafeCommand, 0))
		{
			StrafeCommand = -localVelocity.x * InertiaDampenerStrength;
		}
	}

	private void ClampCommands()
	{
		ForwardBackCommand = Mathf.Clamp(ForwardBackCommand, -1f, 1f);
		StrafeCommand = Mathf.Clamp(StrafeCommand, -1f, 1f);
		RotateCommand = Mathf.Clamp(RotateCommand, -1f, 1f);
	}

	private void SendThrustCommands()
	{
		_thrusterResponses.Clear();

		LinearThruster[] linearThrusters = GetComponentsInChildren<LinearThruster>();
		foreach (LinearThruster thruster in linearThrusters)
		{
			thruster.SetCommands(ForwardBackCommand, StrafeCommand, RotateCommand);
			_thrusterResponses.Add(thruster.GetResponse());
		}
	}

	#endregion

	#region Resources

	public int GetResourcePriority()
	{
		return 0;
	}

	public Dictionary<VehicleResource, float> GetConsumptionRateRequest()
	{
		_resourceRequests.Clear();
		DictionaryUtils.AddDictionaries(
			_thrusterResponses.Select(response => response.ResourceConsumptionRateRequest),
			_resourceRequests
		);
		return _resourceRequests;
	}

	public void SetSatisfactionLevel(float satisfaction)
	{
		foreach (ThrusterResponse response in _thrusterResponses)
		{
			_body.AddForceAtPosition(response.Force * satisfaction, response.ForceOrigin);
		}

		_resourceSatisfaction = satisfaction;
	}

	private void LateUpdate()
	{
		foreach (LinearThruster thruster in GetComponentsInChildren<LinearThruster>())
		{
			thruster.PlayEffect(_resourceSatisfaction);
		}
	}

	#endregion

	private void ToggleInertiaDampener(InputAction.CallbackContext context)
	{
		_inertiaDampenerActive = !_inertiaDampenerActive;
		UpdateUserInterface();
	}

	private void UpdateUserInterface()
	{
		string inertiaDampenerStatus =
			_inertiaDampenerActive ? "<color=\"green\">ON</color>" : "<color=\"red\">OFF</color>";
		InertiaDampenerStatusIndicator.text = $"Inertia Dampener {inertiaDampenerStatus}";
	}
}
}