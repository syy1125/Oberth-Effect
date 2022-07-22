using System.Linq;

namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidateRangeFloatAttribute : AbstractValidationAttribute
{
	private readonly float _min;
	private readonly float _max;

	public ValidateRangeFloatAttribute(float min, float max)
	{
		_min = min;
		_max = max;
	}

	public override void Validate(object value)
	{
		if (value == null) return;

		if (value is float floatValue)
		{
			if (floatValue < _min || floatValue > _max)
			{
				throw new ValidationError($"should be in the range [{_min}, {_max}]");
			}
		}
		else if (value is float[] floatArrayValue)
		{
			if (floatArrayValue.Any(f => f < _min || f > _max))
			{
				throw new ValidationError($"all elements should be in the range [{_min}, {_max}]");
			}
		}
		else
		{
			throw new ValidationError("should be a floating point value");
		}
	}
}
}