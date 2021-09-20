using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class CameraFollow : MonoBehaviour
{
	public Transform Target;
	public bool FollowCenterOfMass;

	public bool UsePid = true;
	public PidConfig PositionPidConfig;

	private Pid<Vector2> _pid;
	private Vector2 _velocity;

	public float InitTime;

	private float _initTimer;
	private Vector2 _initVelocity;

	private void Awake()
	{
		_pid = new Pid<Vector2>(
			PositionPidConfig,
			(a, b) => a + b,
			(a, b) => a - b,
			(v, s) => v * s
		);
	}

	public void EnterInitMode()
	{
		_initTimer = InitTime;
	}

	private void FixedUpdate()
	{
		if (Target == null) return;

		var body = Target.GetComponent<Rigidbody2D>();

		Vector2 targetPosition = FollowCenterOfMass && body != null
			? body.worldCenterOfMass
			: new Vector2(Target.position.x, Target.position.y);
		Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);

		if (_initTimer > 0)
		{
			_initTimer -= Time.fixedDeltaTime;
			Vector2 position = Vector2.SmoothDamp(currentPosition, targetPosition, ref _initVelocity, InitTime);
			transform.position = new Vector3(position.x, position.y, transform.position.z);
		}
		else if (UsePid)
		{
			Vector2 offset = targetPosition - currentPosition;
			_pid.Update(offset, Time.fixedDeltaTime);
			_velocity += _pid.Output * Time.fixedDeltaTime;
			transform.position += new Vector3(_velocity.x, _velocity.y) * Time.fixedDeltaTime;
		}
		else
		{
			transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
		}
	}

	public void ResetPosition()
	{
		var body = Target.GetComponent<Rigidbody2D>();

		Vector2 targetPosition = FollowCenterOfMass && body != null
			? body.worldCenterOfMass
			: new Vector2(Target.position.x, Target.position.y);

		transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);

		_pid.Reset();
		_velocity = body != null ? body.velocity : Vector2.zero;
	}
}
}