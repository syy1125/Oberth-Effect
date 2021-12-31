using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Validation;

namespace Syy1125.OberthEffect.Spec.Block.Resource
{
public class ResourceSpec : ICustomValidation
{
	public Dictionary<string, float> StorageCapacity;
	public ResourceGeneratorSpec ResourceGenerator;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(StorageCapacity));
		ValidationHelper.ValidateResourceDictionary(path, StorageCapacity, errors);
		path.RemoveAt(path.Count - 1);
	}
}
}