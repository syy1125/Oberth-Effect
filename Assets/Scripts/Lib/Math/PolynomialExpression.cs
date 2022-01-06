using System;
using System.Collections.Generic;
using System.Linq;

namespace Syy1125.OberthEffect.Lib.Math
{
public class PolynomialExpression : IExpression
{
	private readonly float[] _coefficients;

	private PolynomialExpression _derivative;

	public PolynomialExpression(IEnumerable<float> coefficients)
	{
		if (coefficients == null) throw new ArgumentException(nameof(coefficients));
		_coefficients = coefficients.ToArray();
	}

	public PolynomialExpression(params float[] coefficients)
	{
		_coefficients = coefficients.ToArray();
	}

	public float Evaluate(float x)
	{
		float multiplier = 1f;
		float result = 0f;

		foreach (float coefficient in _coefficients)
		{
			result += coefficient * multiplier;
			multiplier *= x;
		}

		return result;
	}

	public bool IsDifferentiable()
	{
		return true;
	}

	public IExpression GetDerivative()
	{
		return _derivative ??= new PolynomialExpression(
			_coefficients
				.Select((coefficient, power) => coefficient * power)
				.Skip(1)
		);
	}

	public override string ToString()
	{
		if (_coefficients.Length == 0) return "0";

		return string.Join(
			"+",
			_coefficients
				.Select(
					(coefficient, power) =>
					{
						return Tuple.Create(
							coefficient,
							power switch
							{
								0 => coefficient.ToString("g4"),
								1 => $"{coefficient:g4}x",
								_ => $"{coefficient:g4}x^{power}"
							}
						);
					}
				)
				.Where(tuple => tuple.Item1 != 0)
				.Select(tuple => tuple.Item2)
		);
	}
}
}