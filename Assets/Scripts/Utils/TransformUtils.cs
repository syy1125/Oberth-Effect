using System;
using PlasticGui.WorkspaceWindow;
using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class TransformUtils
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
			1 => new Vector2Int(position.y, -position.x),
			2 => new Vector2Int(-position.x, -position.y),
			3 => new Vector2Int(-position.y, position.x),
			_ => throw new ArgumentException()
		};
	}

	public static Vector2Int RotatePoint(Vector2Int position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2Int(position.x, position.y),
			1 => new Vector2Int(position.y, -position.x),
			2 => new Vector2Int(-position.x, -position.y),
			3 => new Vector2Int(-position.y, position.x),
			_ => throw new ArgumentException()
		};
	}

	public static Vector2 RotatePoint(Vector3 position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2(position.x, position.y),
			1 => new Vector2(position.y, -position.x),
			2 => new Vector2(-position.x, -position.y),
			3 => new Vector2(-position.y, position.x),
			_ => throw new ArgumentException()
		};
	}

	public static Vector2 RotatePoint(Vector2 position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2(position.x, position.y),
			1 => new Vector2(position.y, -position.x),
			2 => new Vector2(-position.x, -position.y),
			3 => new Vector2(-position.y, position.x),
			_ => throw new ArgumentException()
		};
	}
}
}