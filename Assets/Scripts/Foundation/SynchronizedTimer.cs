using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
{
/// <summary>
/// A custom time synchronization script that smoothes <c>PhotonNetwork.Time</c>.
/// <br/>
/// Automatically shifts the time so that, as nearly as possible, level load is 0 time.
/// <br/>
/// A smoothed time works better for orbit simulations, where small offsets in time causes jittering in orbital positions.
/// </summary>
/// <remarks>
/// Updates on <c>FixedUpdate</c>
/// </remarks>
public class SynchronizedTimer : MonoBehaviourPunCallbacks
{
	public static SynchronizedTimer Instance { get; private set; }

	public float DampTime = 1f;

	private uint _referenceTimestamp;
	private float _referenceUnityTime;
	private float _smoothVelocity;

	public float SynchronizedTime { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(this);
		}
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Start()
	{
		if (PhotonNetwork.InRoom)
		{
			_referenceTimestamp = (uint) PhotonNetwork.ServerTimestamp;
			_referenceUnityTime = Time.time;
		}
	}

	public override void OnJoinedRoom()
	{
		_referenceTimestamp = (uint) PhotonNetwork.ServerTimestamp;
		_referenceUnityTime = Time.time;
	}

	private void FixedUpdate()
	{
		if (!PhotonNetwork.InRoom)
		{
			SynchronizedTime = Time.timeSinceLevelLoad;
			return;
		}

		uint deltaTimestamp = (uint) PhotonNetwork.ServerTimestamp - _referenceTimestamp;
		float trueTime = deltaTimestamp / 1000f;
		float targetReferenceTime = Time.time - trueTime;
		_referenceUnityTime = Mathf.SmoothDamp(
			_referenceUnityTime, targetReferenceTime, ref _smoothVelocity, DampTime,
			Mathf.Infinity, Time.fixedDeltaTime
		);
		SynchronizedTime = Time.time - _referenceUnityTime;
	}
}
}