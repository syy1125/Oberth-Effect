using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.Validation.Attributes
{
public class ValidateTextureIdAttribute : AbstractValidationAttribute
{
	public static HashSet<string> ValidIds;

	public override void Validate(object value)
	{
		if (value == null) return;
		
		if (value is string stringValue)
		{
			if (!ValidIds.Contains(stringValue))
			{
				throw new ValidationError("should reference a texture that exists");
			}
		}
		else
		{
			throw new ValidationError("should be a texture id string");
		}
	}
}
}