using System;
using System.Collections;
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
			"+", _coefficients.Select(
				(coefficient, power) =>
				{
					return power switch
					{
						0 => coefficient.ToString("g5"),
						1 => $"{coefficient:g5}x",
						_ => $"{coefficient:g5}x^{power}"
					};
				}
			)
		);
	}
}
}