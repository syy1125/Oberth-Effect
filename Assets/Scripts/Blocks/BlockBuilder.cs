using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public static class BlockBuilder
{
	private static Material _vehicleBlockMaterial;

	public static GameObject BuildFromSpec(
		BlockSpec blockSpec, Transform parent, Vector2Int rootPosition, int rotation, in BlockContext context
	)
	{
		var blockObject = new GameObject(blockSpec.Info.FullName);

		Transform blockTransform = blockObject.transform;
		blockTransform.SetParent(parent);
		blockTransform.localPosition = new Vector3(rootPosition.x, rootPosition.y, 0f);
		blockTransform.localRotation = TransformUtils.GetPhysicalRotation(rotation);
		blockTransform.localScale = Vector3.one;

		RendererHelper.AttachRenderers(blockTransform, blockSpec.Renderers);

		if (blockSpec.Physics.BoxCollider != null)
		{
			var boxCollider = blockObject.AddComponent<BoxCollider2D>();
			boxCollider.LoadSpec(blockSpec.Physics.BoxCollider);
		}

		if (blockSpec.Physics.PolygonCollider != null)
		{
			var polygonCollider = blockObject.AddComponent<PolygonCollider2D>();
			polygonCollider.LoadSpec(blockSpec.Physics.PolygonCollider);
		}

		blockObject.AddComponent<ColorSchemePainter>();

		var blockCore = blockObject.AddComponent<BlockCore>();
		blockCore.BlockId = blockSpec.BlockId;
		blockCore.RootPosition = rootPosition;
		blockCore.Rotation = rotation;
		blockCore.CenterOfMassPosition =
			rootPosition + TransformUtils.RotatePoint(blockSpec.Physics.CenterOfMass, rotation);

		var blockInfo = blockObject.AddComponent<BlockInfoTooltip>();
		blockInfo.LoadSpec(blockSpec);

		var blockHealth = blockObject.AddComponent<BlockHealth>();
		blockHealth.LoadSpec(blockSpec);

		if (blockSpec.ControlCore != null)
		{
			blockObject.AddComponent<ControlCore>();
		}

		foreach (var entry in blockSpec.BlockComponents)
		{
			LoadModComponent(blockObject, entry, context);
		}

		var blockDescription = blockObject.AddComponent<BlockDescription>();
		blockDescription.LoadSpec(blockSpec);

		LayerUtils.SetLayerRecursively(
			blockObject, LayerConstants.VEHICLE_BLOCK_LAYER, LayerConstants.VEHICLE_LAYER_MASK
		);

		blockObject.GetComponent<ColorSchemePainter>()?.ApplyColorScheme();

		return blockObject;
	}

	private static void LoadModComponent(GameObject blockObject, KeyValuePair<string, object> entry, BlockContext context)
	{
		var componentType = BlockSpec.GetComponentType(entry.Key);
		var specType = BlockSpec.GetSpecType(entry.Key);
		if (componentType == null || specType == null)
		{
			Debug.LogError($"Failed to find component types for \"{entry.Key}\"");
			return;
		}

		if (!specType.IsInstanceOfType(entry.Value))
		{
			Debug.LogError(
				$"Received spec under key \"{entry.Key}\" but it is not of type `{specType.FullName}`. You may have forgotten to tag the component spec (\"!{entry.Key}\")."
			);
			return;
		}

		var component = blockObject.AddComponent(componentType);

		if (!typeof(IBlockComponent<>).MakeGenericType(specType).IsAssignableFrom(componentType))
		{
			Debug.LogError(
				$"`{componentType.FullName}` does not implement `IBlockComponent<{specType.FullName}>`. Skipping spec loading."
			);
			return;
		}

		componentType.GetMethod(nameof(IBlockComponent<object>.LoadSpec)).Invoke(component, new[] { entry.Value, context });
	}
}
}