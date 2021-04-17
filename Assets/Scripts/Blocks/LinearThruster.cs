using UnityEngine;

public class LinearThruster : BlockBehaviour
{
	public float MaxForce;

	private Rigidbody2D _body;
	private VehicleThrusterControl _control;
	private ParticleSystem _particles;

	private float _forwardBackResponse;
	private float _strafeResponse;
	private float _rotateResponse;
	private float _maxParticleSpeed;

	private void Awake()
	{
		_body = GetComponentInParent<Rigidbody2D>();
		_control = GetComponentInParent<VehicleThrusterControl>();
		_particles = GetComponent<ParticleSystem>();
	}

	private void Start()
	{
		if (HasPhysics)
		{
			Vector3 localUp = transform.localRotation * Vector3.up;
			Vector3 localPosition = transform.localPosition - (Vector3) _body.centerOfMass;

			_forwardBackResponse = localUp.y;
			_strafeResponse = localUp.x;
			_rotateResponse = localUp.x * localPosition.y - localUp.y * localPosition.x;

			_rotateResponse = Mathf.Abs(_rotateResponse) > 1e-5 ? Mathf.Sign(_rotateResponse) : 0f;

			if (_particles != null)
			{
				_maxParticleSpeed = _particles.main.startSpeedMultiplier;
				_particles.Play();
			}
		}
	}

	private void FixedUpdate()
	{
		if (HasPhysics)
		{
			float rawResponse = _forwardBackResponse * _control.ForwardBackCommand
			                    + _strafeResponse * _control.StrafeCommand
			                    + _rotateResponse * _control.RotateCommand;
			float response = Mathf.Clamp01(rawResponse);

			_body.AddForceAtPosition(response * MaxForce * transform.up, transform.position);

			if (_particles != null)
			{
				ParticleSystem.MainModule main = _particles.main;
				main.startSpeedMultiplier = response * _maxParticleSpeed;
				main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, response));
			}
		}
	}
}