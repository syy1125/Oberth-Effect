using System.IO;

namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidateFilePathAttribute : AbstractValidationAttribute
{
	public override void Validate(object value)
	{
		if (value == null) return;

		if (value is string stringValue)
		{
			if (!File.Exists(stringValue))
			{
				throw new ValidationError($"references file at {stringValue} which does not exist");
			}
		}
		else
		{
			throw new ValidationError("should be a file path string");
		}
	}
}
}