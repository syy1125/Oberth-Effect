using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class TransformUtils
{
	// Some notes
	// Rotation is counterclockwise

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

	public static Vector2 RotatePoint(Vector3 position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2(position.x, position.y),
			1 => new Vector2(-position.y, position.x),
			2 => new Vector2(-position.x, -position.y),
			3 => new Vector2(position.y, -position.x),
			_ => throw new ArgumentException()
		};
	}

	public static Vector2 RotatePoint(Vector2 position, int rotation)
	{
		return rotation switch
		{
			0 => new Vector2(position.x, position.y),
			1 => new Vector2(-position.y, position.x),
			2 => new Vector2(-position.x, -position.y),
			3 => new Vector2(position.y, -position.x),
			_ => throw new ArgumentException()
		};
	}

	public static BoundsInt TransformBounds(BoundsInt bounds, Vector2Int rootLocation, int rotation)
	{
		BoundsInt output = new BoundsInt();

		// There are some +1's in there to adjust for how BoundsInt is inclusive min and exclusive max
		switch (rotation)
		{
			case 0:
				output.SetMinMax((Vector3Int) rootLocation + bounds.min, (Vector3Int) rootLocation + bounds.max);
				break;
			case 1:
				output.xMin = rootLocation.x - bounds.yMax + 1;
				output.yMin = rootLocation.y + bounds.xMin;
				output.xMax = rootLocation.x - bounds.yMin + 1;
				output.yMax = rootLocation.y + bounds.xMax;
				output.zMin = 0;
				output.zMax = 1;
				break;
			case 2:
				output.xMin = rootLocation.x - bounds.xMax + 1;
				output.yMin = rootLocation.y - bounds.yMax + 1;
				output.xMax = rootLocation.x - bounds.xMin + 1;
				output.yMax = rootLocation.y - bounds.yMin + 1;
				output.zMin = 0;
				output.zMax = 1;
				break;
			case 3:
				output.xMin = rootLocation.x + bounds.yMin;
				output.yMin = rootLocation.y - bounds.xMax + 1;
				output.xMax = rootLocation.x + bounds.yMax;
				output.yMax = rootLocation.y - bounds.xMin + 1;
				output.zMin = 0;
				output.zMax = 1;
				break;
			default:
				throw new ArgumentException();
		}

		return output;
	}
}
}