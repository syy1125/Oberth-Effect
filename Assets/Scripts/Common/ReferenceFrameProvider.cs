using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
public class ReferenceFrameProvider : MonoBehaviour
{
	private Rigidbody2D _body;
	public static readonly ICollection<ReferenceFrameProvider> ReferenceFrames = new HashSet<ReferenceFrameProvider>();
	public Vector2 PrevPosition { get; private set; }
	public float PrevRotation { get; private set; }

	private void Awake()
	{
		ReferenceFrames.Add(this);
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

	public float EstimateMinApproachDistanceDuringFrame(Vector2 start, Vector2 end)
	{
		start -= PrevPosition;
		end -= (Vector2) transform.position;

		// Project start -> origin vector onto start -> end vector and find t, the interpolation factor
		Vector2 line = end - start;
		if (Mathf.Approximately(line.sqrMagnitude, 0f)) return start.magnitude;
		float t = -Vector2.Dot(start, line) / line.sqrMagnitude;

		// If t is within [0,1], the perpendicular line is the shortest. Otherwise, one of the endpoints is the closes.
		if (t <= 0)
		{
			return start.magnitude;
		}
		else if (t >= 1)
		{
			return end.magnitude;
		}
		else
		{
			return Vector2.Lerp(start, end, t).magnitude;
		}
	}

	public struct RayStep
	{
		// T takes on values of [0,1] corresponding to times of "start of frame" to "end of frame".
		public float T;
		public Vector2 WorldStart;
		public Vector2 WorldEnd;
		public GameObject Parent;
	}

	public RayStep[] GetRaySteps(Vector2 start, Vector2 end)
	{
		GameObject parent = gameObject;
		Vector2 currentPosition = transform.position;
		float currentRotation = _body == null ? 0f : _body.rotation;

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
				Vector2.Lerp(start, end, t) + currentPosition,
				Mathf.Lerp(PrevRotation, currentRotation, t)
			);
		}

		RayStep[] steps = new RayStep[stepCount];
		for (int i = 0; i < stepCount; i++)
		{
			steps[i] = new RayStep
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