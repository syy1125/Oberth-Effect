namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidateRangeIntAttribute : AbstractValidationAttribute
{
	private readonly int _min;
	private readonly int _max;

	public ValidateRangeIntAttribute(int min, int max)
	{
		_min = min;
		_max = max;
	}

	public override void Validate(object value)
	{
		if (value == null) return;

		if (value is int intValue)
		{
			if (intValue < _min || intValue > _max)
			{
				throw new ValidationError($"should be in the range [{_min}, {_max}]");
			}
		}
		else
		{
			throw new ValidationError("should be an integer");
		}
	}
}
}