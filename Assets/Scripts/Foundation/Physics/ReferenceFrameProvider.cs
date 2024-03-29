﻿using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation.Physics
{
[RequireComponent(typeof(PhotonView))]
public class ReferenceFrameProvider : MonoBehaviourPun
{
	public static ReferenceFrameProvider MainReferenceFrame;

	public bool IsMine { get; private set; }
	private Rigidbody2D _body;
	public static readonly ICollection<ReferenceFrameProvider> ReferenceFrames = new HashSet<ReferenceFrameProvider>();
	public Vector2 PrevPosition { get; private set; }
	public float PrevRotation { get; private set; }

	private void Awake()
	{
		ReferenceFrames.Add(this);
		IsMine = photonView.IsMine;
		_body = GetComponent<Rigidbody2D>();
	}

	private void Start()
	{
		PrevRotation = 0f;
	}

	private void FixedUpdate()
	{
		PrevPosition = transform.position;
		if (_body != null) PrevRotation = _body.rotation;
	}

	private void OnDestroy()
	{
		ReferenceFrames.Remove(this);
	}

	public Vector2 GetVelocity()
	{
		if (_body != null)
		{
			return _body.velocity;
		}
		else
		{
			return ((Vector2) transform.position - PrevPosition) / Time.fixedDeltaTime;
		}
	}

	public float GetMinApproachSqrDistance(Vector2 start, Vector2 end)
	{
		start -= PrevPosition;
		end -= (Vector2) transform.position;

		// Project start -> origin vector onto start -> end vector and find t, the interpolation factor
		Vector2 line = end - start;
		if (Mathf.Approximately(line.sqrMagnitude, 0f)) return start.sqrMagnitude;
		float t = -Vector2.Dot(start, line) / line.sqrMagnitude;

		// If t is within [0,1], the perpendicular line is the shortest. Otherwise, one of the endpoints is the closest.
		if (t <= 0)
		{
			return start.sqrMagnitude;
		}
		else if (t >= 1)
		{
			return end.sqrMagnitude;
		}
		else
		{
			return Vector2.Lerp(start, end, t).sqrMagnitude;
		}
	}

	public struct RayStep
	{
		// T takes on values of [0,1] corresponding to times of "start of frame" to "end of frame".
		public float T;
		public Vector2 WorldStart;
		public Vector2 WorldEnd;
		public Transform Parent;
	}

	public RayStep[] GetRaySteps(Vector2 start, Vector2 end)
	{
		Transform parent = transform;
		Vector2 currentPosition = transform.position;

		float currentRotation = _body == null ? 0f : _body.rotation;
		float rotationDelta = currentRotation - PrevRotation;

		// Transform to local position (but not rotated)
		start -= PrevPosition;
		end -= currentPosition;
		Vector2 line = end - start;

		int stepCount = Mathf.Max(Mathf.CeilToInt(line.magnitude), 1);
		Vector2[] worldPoints = new Vector2[stepCount + 1];

		for (int i = 0; i <= stepCount; i++)
		{
			// Unity's rigidbody.rotation doesn't seem to wrap around
			// (tested up to ~5,000,000 deg, takes a few minutes as the ang vel cap of 18000 to reach)
			// So we should be able to safely interpolate here.
			float t = (float) i / stepCount;
			worldPoints[i] = RotateVector(
				                 Vector2.Lerp(start, end, t),
				                 rotationDelta * (1 - t)
			                 )
			                 + currentPosition;
		}

		RayStep[] steps = new RayStep[stepCount];
		for (int i = 0; i < stepCount; i++)
		{
			steps[i] = new()
			{
				T = (float) i / stepCount,
				WorldStart = worldPoints[i],
				WorldEnd = worldPoints[i + 1],
				Parent = parent
			};
		}

		return steps;
	}

	private Vector2 RotateVector(Vector2 v, float rotation)
	{
		float rad = rotation * Mathf.Deg2Rad;
		float sin = Mathf.Sin(rad), cos = Mathf.Cos(rad);

		return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
	}
}
}