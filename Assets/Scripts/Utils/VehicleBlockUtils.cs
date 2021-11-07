using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Utils
{
public static class VehicleBlockUtils
{
	public static IEnumerable<Vector2Int> AllPositionsOccupiedBy(BlockSpec spec, Vector2Int rootLocation, int rotation)
	{
		foreach (Vector3Int localPosition in new BlockBounds(spec.Construction.BoundsMin, spec.Construction.BoundsMax)
			.AllPositionsWithin)
		{
			yield return rootLocation + TransformUtils.RotatePoint(localPosition, rotation);
		}
	}

	public static IEnumerable<Vector2Int> GetAttachmentPoints(VehicleBlueprint.BlockInstance blockInstance)
	{
		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec;

		foreach (Vector2Int attachmentPoint in spec.Construction.AttachmentPoints)
		{
			yield return blockInstance.Position + TransformUtils.RotatePoint(attachmentPoint, blockInstance.Rotation);
		}
	}

	// Get all blocks that are "fully connected" to the given block.
	// Two blocks are "fully connected" if both of them has at least one attachment point inside the other block.
	public static IEnumerable<VehicleBlueprint.BlockInstance> GetConnectedBlocks(
		VehicleBlueprint.BlockInstance block, IReadOnlyDictionary<Vector2Int, VehicleBlueprint.BlockInstance> posToBlock
	)
	{
		HashSet<VehicleBlueprint.BlockInstance> tested = new HashSet<VehicleBlueprint.BlockInstance>();

		foreach (Vector2Int attachmentPoint in GetAttachmentPoints(block))
		{
			if (posToBlock.TryGetValue(attachmentPoint, out VehicleBlueprint.BlockInstance target))
			{
				if (!tested.Add(target)) continue;

				// A block is connected if it "connects back" to one of the originator's positions
				if (BlockConnects(target, block, posToBlock))
				{
					yield return target;
				}
			}
		}
	}

	private static bool BlockConnects(
		VehicleBlueprint.BlockInstance fromBlock, VehicleBlueprint.BlockInstance toBlock,
		IReadOnlyDictionary<Vector2Int, VehicleBlueprint.BlockInstance> posToBlock
	)
	{
		foreach (Vector2Int attachmentPoint in GetAttachmentPoints(fromBlock))
		{
			if (posToBlock.TryGetValue(attachmentPoint, out VehicleBlueprint.BlockInstance targetBlock)
			    && targetBlock == toBlock)
			{
				return true;
			}
		}

		return false;
	}
}
}