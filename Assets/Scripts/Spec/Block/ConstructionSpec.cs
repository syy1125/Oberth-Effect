using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct ConstructionSpec : ICustomValidation
{
	public bool ShowInDesigner;
	public bool AllowErase;

	public Vector2Int BoundsMin;
	public Vector2Int BoundsMax;
	[SchemaDescription("Defines a set of points that this block can attach to. Two blocks are connected if and only if they each have at least one attachment point in the other block's bounds.")]
	public Vector2Int[] AttachmentPoints;

	[RequireChecksumLevel(ChecksumLevel.Everything)]
	[ValidateBlockId]
	public string MirrorBlockId;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public Vector2Int MirrorRootOffset;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public int MirrorRotationOffset;

	public void Validate(List<string> path, List<string> errors)
	{
		ValidationHelper.ValidateFields(path, this, errors);

		if (BoundsMin.x > 0 || BoundsMin.y > 0 || BoundsMax.x < 1 || BoundsMax.y < 1)
		{
			path.Add("Bounds");
			errors.Add(ValidationHelper.FormatValidationError(path, "should contain origin point (0,0)"));
			path.RemoveAt(path.Count - 1);
		}
	}
}
}