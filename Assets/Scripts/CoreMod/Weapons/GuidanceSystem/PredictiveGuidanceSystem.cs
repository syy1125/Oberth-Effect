using System;
using System.Collections;
using Photon.Pun;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.CoreMod.Weapons.Launcher;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Pid;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons.GuidanceSystem
{
public enum MissileRetargetingBehaviour
{
	Never,
	IfInvalid,
	Always
}

public class PredictiveGuidanceSystemSpec : IGuidanceSystemSpec
{
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxAcceleration;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxAngularAcceleration;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float ThrustActivationDelay;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float GuidanceActivationDelay;
	public MissileRetargetingBehaviour RetargetingBehaviour = MissileRetargetingBehaviour.Never;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public ParticleSystemSpec[] PropulsionParticles;

	public float GetMaxRange(float initialSpeed, float lifetime)
	{
		return initialSpeed * lifetime + 0.5f * MaxAcceleration * lifetime * lifetime;
	}

	public string GetGuidanceSystemTooltip()
	{
		return
			$"    Max acceleration {PhysicsUnitUtils.FormatAcceleration(MaxAcceleration)}, max angular acceleration {MaxAngularAcceleration:0.#}°/s²";
	}

	public Vector2? GetInterceptPoint(
		Vector2 ownPosition, Vector2 ownVelocity, Vector2 targetPosition, Vector2 targetVelocity
	)
	{
		Vector2 relativePosition = targetPosition - ownPosition;
		Vector2 relativeVelocity = targetVelocity - ownVelocity;

		if (
			InterceptSolver.MissileIntercept(
				relativePosition, relativeVelocity, MaxAcceleration, out Vector2 acceleration, out float hitTime
			)
		)
		{
			return ownPosition + 0.5f * acceleration * hitTime * hitTime;
		}
		else
		{
			return null;
		}
	}
}

public class PredictiveGuidanceSystem :
	MonoBehaviourPun,
	INetworkedProjectileComponent<PredictiveGuidanceSystemSpec>,
	IRemoteControlledProjectileComponent,
	IMissileAlertSource,
	IProjectileLifecycleListener,
	IPunObservable
{
	public AbstractWeaponLauncher Launcher { get; set; }

	private Rigidbody2D _ownBody;

	private IGuidedWeaponTarget _target;
	private float _maxAcceleration;
	private float _maxAngularAcceleration;
	private float _thrustActivationDelay;
	private float _guidanceActivationDelay;
	private MissileRetargetingBehaviour _retargetingBehaviour;
	private ParticleSystemWrapper[] _propulsionParticles;

	private float _initTime;
	private Vector2? _desiredAcceleration;
	private IPid<float> _rotationPid;

	private float? _hitTime;

	private void Awake()
	{
		_ownBody = GetComponent<Rigidbody2D>();

		_hitTime = null;
	}

	public void LoadSpec(PredictiveGuidanceSystemSpec spec)
	{
		if (spec.PropulsionParticles != null)
		{
			_propulsionParticles = RendererHelper.CreateParticleSystems(transform, spec.PropulsionParticles);
		}

		_maxAcceleration = spec.MaxAcceleration;
		_maxAngularAcceleration = spec.MaxAngularAcceleration;
		_thrustActivationDelay = spec.ThrustActivationDelay;
		_guidanceActivationDelay = spec.GuidanceActivationDelay;
		_retargetingBehaviour = spec.RetargetingBehaviour;

		_initTime = Time.time;
		_desiredAcceleration = null;
		_rotationPid = new RotationPid(
			new()
			{
				Response = 0.1f,
				DerivativeTime = 5f / Mathf.Sqrt(spec.MaxAngularAcceleration)
			}
		);
	}

	public void AfterSpawn()
	{}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());

		if (_propulsionParticles != null)
		{
			foreach (ParticleSystemWrapper particle in _propulsionParticles)
			{
				particle.Play();
			}
		}
	}

	private void FixedUpdate()
	{
		if (_desiredAcceleration == null)
		{
			ApplyThrust(Time.time - _initTime < _thrustActivationDelay ? 0f : _maxAcceleration);
		}
		else
		{
			// If guidance is active (indicated by desired acceleration being non-null), the missile will try to rotate toward its target.
			float angle = Vector2.SignedAngle(_desiredAcceleration.Value, transform.up);
			_rotationPid.Update(angle, Time.fixedDeltaTime);

			float rotationResponse = Mathf.Clamp(_rotationPid.Output, -1f, 1f);
			_ownBody.angularVelocity -= rotationResponse * _maxAngularAcceleration * Time.fixedDeltaTime;

			if (Time.time - _initTime < _thrustActivationDelay)
			{
				ApplyThrust(0f);
			}
			else
			{
				float cos = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));
				float thrustFraction = cos * cos;
				ApplyThrust(thrustFraction * _desiredAcceleration.Value.magnitude);
			}
		}
	}

	private void ApplyThrust(float acceleration)
	{
		_ownBody.velocity += (Vector2) transform.up * (acceleration * Time.deltaTime);

		float thrustScale = acceleration / _maxAcceleration;

		if (_propulsionParticles != null)
		{
			ParticleSystemWrapper.BatchScaleThrustParticles(_propulsionParticles, thrustScale);
		}
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitWhile(() => Time.time - _initTime < _guidanceActivationDelay);

		if (photonView.IsMine)
		{
			if (Launcher == null)
			{
				if (!HasValidTarget())
				{
					Debug.LogWarning(
						"PredictiveGuidanceSystem did not receive a weapon launcher reference and may not seek target correctly."
					);
				}
			}
			else
			{
				SetTargetId(Launcher.TargetPhotonId);
				Debug.Log($"Missile launched with TargetPhotonId={Launcher.TargetPhotonId?.ToString() ?? "null"}");
			}
		}

		while (isActiveAndEnabled)
		{
			yield return new WaitForFixedUpdate();

			if (photonView.IsMine)
			{
				switch (_retargetingBehaviour)
				{
					case MissileRetargetingBehaviour.Never:
						break;
					case MissileRetargetingBehaviour.IfInvalid:
						if (!HasValidTarget())
						{
							RetargetMissile();
						}

						break;
					case MissileRetargetingBehaviour.Always:
						RetargetMissile();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				SolveGuidance();
			}
		}
	}

	private void RetargetMissile()
	{
		if (Launcher == null || Launcher.Equals(null) || !Launcher.enabled)
		{
			SetTargetId(null);
			return;
		}

		if (Launcher.TargetPhotonId == null)
		{
			if (HasValidTarget())
			{
				SetTargetId(null);
			}
		}
		else
		{
			if (!HasValidTarget() || Launcher.TargetPhotonId.Value != _target.photonView.ViewID)
			{
				SetTargetId(Launcher.TargetPhotonId);
			}
		}
	}

	private void SolveGuidance()
	{
		if (!HasValidTarget())
		{
			_desiredAcceleration = transform.up * _maxAcceleration;
		}
		else
		{
			Vector2 relativePosition = _target.GetEffectivePosition() - _ownBody.worldCenterOfMass;
			Vector2 relativeVelocity = _target.GetEffectiveVelocity() - _ownBody.velocity;

			if (
				InterceptSolver.MissileIntercept(
					relativePosition, relativeVelocity, _maxAcceleration,
					out Vector2 acceleration, out float hitTime
				)
			)
			{
				_desiredAcceleration = acceleration;
				_hitTime = hitTime;
			}
			else
			{
				_hitTime = null;
			}
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(GetTargetId());
		}
		else
		{
			var targetId = (int?) stream.ReceiveNext();
			if (targetId != GetTargetId())
			{
				SetTargetId(targetId);
			}
		}
	}

	private int? GetTargetId()
	{
		return HasValidTarget() ? _target.photonView.ViewID : null;
	}

	private void SetTargetId(int? targetId)
	{
		var prevTarget = _target;
		_target = targetId == null ? null : PhotonView.Find(targetId.Value)?.GetComponent<IGuidedWeaponTarget>();

		MissileAlertSystem.OnTargetChanged(this, prevTarget, _target);
	}

	private bool HasValidTarget()
	{
		// Unity's nullability system doesn't play nice with System.Object.Equals. It gets really finicky, this is the best I've found so far.
		return _target != null && !_target.Equals(null);
	}

	public float? GetHitTime()
	{
		return _hitTime;
	}

	public void BeforeDespawn()
	{
		if (HasValidTarget())
		{
			foreach (var receiver in _target.GetComponents<IMissileAlertReceiver>())
			{
				receiver.RemoveIncomingMissile(this);
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (_desiredAcceleration != null)
		{
			Gizmos.matrix = Matrix4x4.identity;
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, transform.position + (Vector3) _desiredAcceleration.Value);
			Gizmos.color = new Color(1f, 0.5f, 0f);
			float angle = Vector2.SignedAngle(_desiredAcceleration.Value, transform.up);
			float cos = Mathf.Clamp01(Mathf.Cos(angle * Mathf.Deg2Rad));
			Gizmos.DrawLine(
				transform.position,
				transform.TransformPoint(new Vector2(0f, cos * cos * _desiredAcceleration.Value.magnitude))
			);
		}
	}
}
}