using Syy1125.OberthEffect.Simulation.Construct;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CameraFollow))]
public class VehicleCameraSizeControl : MonoBehaviour
{
	private Camera _camera;
	private CameraFollow _follow;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		_follow = GetComponent<CameraFollow>();
	}

	private void LateUpdate()
	{
		if (_follow.Target == null) return;

		Rigidbody2D body = _follow.Target.GetComponent<Rigidbody2D>();
		ConstructBlockManager blockManager = _follow.Target.GetComponent<ConstructBlockManager>();

		Vector2 com = body.centerOfMass;
		BoundsInt vehicleBounds = blockManager.GetBounds();
		float xSize = Mathf.Max(vehicleBounds.xMax - 1 - com.x, com.x - vehicleBounds.xMin) + 1;
		float ySize = Mathf.Max(vehicleBounds.yMax - 1 - com.y, com.y - vehicleBounds.yMin) + 1;

		_camera.orthographicSize = Mathf.Max(xSize, ySize);
	}
}
}