using System;
using UnityEngine;

public class RotationUtils
{
	public static Quaternion GetPhysicalRotation(int rotation)
	{
		return Quaternion.AngleAxis(rotation * 90f, Vector3.forward);
	}

	public static Vector2Int RotatePoint(Vector3Int position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2Int(position.x, position.y),
			1 => new Vector2Int(-position.y, position.x),
			2 => new Vector2Int(-position.x, -position.y),
			3 => new Vector2Int(position.y, -position.x),
			_ => throw new ArgumentException()
		};
	}

	public static Vector2Int RotatePoint(Vector2Int position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2Int(position.x, position.y),
			1 => new Vector2Int(-position.y, position.x),
			2 => new Vector2Int(-position.x, -position.y),
			3 => new Vector2Int(position.y, -position.x),
			_ => throw new ArgumentException()
		};
	}
}