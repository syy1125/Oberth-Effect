using UnityEngine;

namespace Syy1125.OberthEffect.Lib.Pid
{
public class Vector2Pid : Pid<Vector2>
{
	public Vector2Pid(PidConfig config) : base(config, Add, Subtract, Multiply)
	{}

	private static Vector2 Add(Vector2 a, Vector2 b) => a + b;

	private static Vector2 Subtract(Vector2 a, Vector2 b) => a - b;

	private static Vector2 Multiply(Vector2 a, float b) => a * b;
}
}