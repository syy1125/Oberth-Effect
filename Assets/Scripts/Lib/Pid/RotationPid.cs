using UnityEngine;

namespace Syy1125.OberthEffect.Lib.Pid
{
public class RotationPid : Pid<float>
{
	public RotationPid(PidConfig config) : base(config, Add, Subtract, Multiply)
	{}

	private static float Add(float a, float b) => a + b;

	private static float Subtract(float a, float b) => Mathf.DeltaAngle(b, a);

	private static float Multiply(float a, float b) => a * b;
}
}