using System.Collections.Generic;

namespace Syy1125.OberthEffect.Spec.Validation
{
public interface ICustomValidation
{
	void Validate(List<string> path, List<string> errors);
}
}