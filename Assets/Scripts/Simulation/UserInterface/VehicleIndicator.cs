using Photon.Pun;
using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class VehicleIndicator : MonoBehaviour
{
	private Rigidbody2D _body;
	private ConstructBlockManager _blockManager;
	private VehicleThrusterControl _thrusterControl;

	public Transform ForwardIndicator;
	public Transform PropulsionIndicator;

	private float _angle;
	private float _angularVelocity;

	private void Awake()
	{
		if (!GetComponentInParent<PhotonView>().IsMine)
		{
			gameObject.SetActive(false);
			return;
		}

		_body = GetComponentInParent<Rigidbody2D>();
		_blockManager = GetComponentInParent<ConstructBlockManager>();
		_thrusterControl = GetComponentInParent<VehicleThrusterControl>();
	}

	private void LateUpdate()
	{
		var com = _body.centerOfMass;
		BoundsInt vehicleBounds = _blockManager.GetBounds();
		ForwardIndicator.localPosition = new Vector3(com.x, vehicleBounds.yMax + 2f, 0f);

		Vector2 playerTranslate = new Vector2(
			_thrusterControl.HorizontalCommand.PlayerValue, _thrusterControl.VerticalCommand.PlayerValue
		);

		if (playerTranslate.sqrMagnitude > Mathf.Epsilon)
		{
			float xSize = Mathf.Max(vehicleBounds.xMax - 1 - com.x, com.x - vehicleBounds.xMin) + 1;
			float ySize = Mathf.Max(vehicleBounds.yMax - 1 - com.y, com.y - vehicleBounds.yMin) + 1;
			float targetAngle = Mathf.Atan2(playerTranslate.y, playerTranslate.x);

			if (!PropulsionIndicator.gameObject.activeSelf)
			{
				_angle = targetAngle;
				PositionPropulsionIndicator(com, new Vector2(xSize, ySize));
				PropulsionIndicator.gameObject.SetActive(true);
			}
			else
			{
				_angle = Mathf.SmoothDampAngle(
					         _angle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg,
					         ref _angularVelocity, 0.02f
				         )
				         * Mathf.Deg2Rad;
				PositionPropulsionIndicator(com, new Vector2(xSize, ySize));
			}
		}
		else
		{
			PropulsionIndicator.gameObject.SetActive(false);
			_angularVelocity = 0f;
		}
	}

	private void PositionPropulsionIndicator(Vector2 com, Vector2 size)
	{
		float cos = Mathf.Cos(_angle);
		float sin = Mathf.Sin(_angle);

		// Polar form of ellipse https://en.wikipedia.org/wiki/Ellipse#Polar_forms
		float r = size.x
		          * size.y
		          / Mathf.Sqrt(
			          Mathf.Pow(size.y * cos, 2)
			          + Mathf.Pow(size.x * sin, 2)
		          )
		          + 1;
		PropulsionIndicator.localPosition = com + new Vector2(cos, sin) * r;
		PropulsionIndicator.localRotation = Quaternion.AngleAxis(_angle * Mathf.Rad2Deg - 90f, Vector3.forward);
	}
}
}