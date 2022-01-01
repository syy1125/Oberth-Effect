using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Lib.Math
{
public static class HalleySolver
{
	public static float FindRoot(
		IExpression expression, float seed, out bool converged, int maxIter = 10, float epsilon = 1e-5f
	)
	{
		if (!expression.IsDifferentiable())
		{
			throw new ArgumentException("Expression is not differentiable");
		}

		IExpression derivative = expression.GetDerivative();
		float root = seed;
		converged = true;

		if (derivative.IsDifferentiable())
		{
			// Halley's method
			// https://en.wikipedia.org/wiki/Halley%27s_method
			IExpression secondOrder = derivative.GetDerivative();

			for (int i = 0; i < maxIter; i++)
			{
				float expressionValue = expression.Evaluate(root);
				if (Mathf.Abs(expressionValue) < epsilon) return root;
				float derivativeValue = derivative.Evaluate(root);
				float secondOrderValue = secondOrder.Evaluate(root);

				float delta = (2 * expressionValue * derivativeValue)
				              / (2 * derivativeValue * derivativeValue - expressionValue * secondOrderValue);
				root -= delta;
			}
		}
		else
		{
			// Fall back to Newton's method
			for (int i = 0; i < maxIter; i++)
			{
				float expressionValue = expression.Evaluate(root);
				if (Mathf.Abs(expressionValue) < epsilon) return root;
				float derivativeValue = derivative.Evaluate(root);

				float delta = expressionValue / derivativeValue;
				root -= delta;
			}
		}

		converged = false;
		return root;
	}
}
}