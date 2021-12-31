namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidateNonNullAttribute : AbstractValidationAttribute
{
	public override void Validate(object value)
	{
		if (value == null)
		{
			throw new ValidationError("should not be null");
		}
	}
}
}