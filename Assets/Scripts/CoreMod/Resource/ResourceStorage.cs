using System.Collections.Generic;
using System.Text;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Resource
{
[CreateSchemaFile("ResourceStorageSpecSchema")]
public class ResourceStorageSpec : ICustomValidation
{
	public Dictionary<string, float> StorageCapacity;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);
		path.Add(nameof(StorageCapacity));
		ValidationHelper.ValidateResourceDictionary(path, StorageCapacity, errors);
		path.RemoveAt(path.Count - 1);
	}
}


public class ResourceStorage : MonoBehaviour,
	IBlockComponent<ResourceStorageSpec>,
	IResourceStorage,
	ITooltipComponent
{
	private Dictionary<string, float> _capacity;

	private void OnEnable()
	{
		GetComponentInParent<IResourceStorageRegistry>()?.RegisterBlock(this);
	}

	public void LoadSpec(ResourceStorageSpec spec, in BlockContext context)
	{
		_capacity = spec.StorageCapacity;
	}

	private void OnDisable()
	{
		GetComponentInParent<IResourceStorageRegistry>()?.UnregisterBlock(this);
	}

	public IReadOnlyDictionary<string, float> GetCapacity()
	{
		return _capacity;
	}

	public bool GetTooltip(StringBuilder builder, string indent)
	{
		builder
			.AppendLine($"{indent}Resource storage capacity")
			.Append($"{indent}  ")
			.AppendLine(string.Join(", ", VehicleResourceDatabase.Instance.FormatResourceDict(_capacity)));

		return true;
	}
}
}