using System.Collections;
using Syy1125.OberthEffect.Lib.Pid;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class CameraFollow : MonoBehaviour
{
	public Transform Target;
	public bool FollowCenterOfMass;

	public bool UsePid = true;
	public PidConfig PositionPidConfig;

	public float InitTime;

	private Vector2 _targetPosition;

	private Pid<Vector2> _pid;
	private Vector2 _velocity;

	private float _initTimer;
	private Vector2 _initVelocity;

	private void Awake()
	{
		_pid = new Vector2Pid(PositionPidConfig);
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	public void SetTarget(GameObject target)
	{
		Target = target.transform;
	}

	public void EnterInitMode()
	{
		_initTimer = InitTime;
	}

	private IEnumerator LateFixedUpdate()
	{
		while (true)
		{
			yield return new WaitForFixedUpdate();

			if (!isActiveAndEnabled) continue;

			if (Target == null) continue;

			if (_initTimer <= 0 && UsePid)
			{
				Vector2 offset = _targetPosition - (Vector2) transform.position;
				_pid.Update(offset, Time.fixedDeltaTime);
				_velocity += _pid.Output * Time.fixedDeltaTime;
				transform.position += new Vector3(_velocity.x, _velocity.y) * Time.fixedDeltaTime;
			}
		}
	}

	private void LateUpdate()
	{
		if (Target == null) return;

		var body = Target.GetComponent<Rigidbody2D>();

		_targetPosition = GetTargetPosition(body);
		Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);

		if (_initTimer > 0)
		{
			_initTimer -= Time.deltaTime;
			Vector2 position = Vector2.SmoothDamp(currentPosition, _targetPosition, ref _initVelocity, InitTime);
			transform.position = new Vector3(position.x, position.y, transform.position.z);
		}
		else if (!UsePid)
		{
			transform.position = new Vector3(_targetPosition.x, _targetPosition.y, transform.position.z);
		}
	}

	public void ResetPosition()
	{
		var body = Target.GetComponent<Rigidbody2D>();

		Vector2 targetPosition = GetTargetPosition(body);

		transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);

		_pid.Reset();
		_velocity = body != null ? body.velocity : Vector2.zero;
		_initTimer = InitTime;
	}

	private Vector2 GetTargetPosition(Rigidbody2D body)
	{
		return FollowCenterOfMass && body != null && body.bodyType != RigidbodyType2D.Static
			? body.worldCenterOfMass
			: new Vector2(Target.position.x, Target.position.y);
	}
}
}