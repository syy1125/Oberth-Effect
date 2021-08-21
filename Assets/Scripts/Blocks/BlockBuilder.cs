using Syy1125.OberthEffect.Blocks.Propulsion;
using Syy1125.OberthEffect.Blocks.Resource;
using Syy1125.OberthEffect.Blocks.Weapons;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public static class BlockBuilder
{
	private static Material _vehicleBlockMaterial;

	public static GameObject BuildFromSpec(BlockSpec blockSpec, Transform parent, Vector2Int rootPosition, int rotation)
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

		if (blockSpec.Resource?.StorageCapacity != null)
		{
			var resourceStorage = blockObject.AddComponent<ResourceStorageBlock>();
			resourceStorage.LoadSpec(blockSpec.Resource.StorageCapacity);
		}

		if (blockSpec.Resource?.FreeGenerator != null)
		{
			var freeGenerator = blockObject.AddComponent<FreeResourceGeneratorBlock>();
			freeGenerator.LoadSpec(blockSpec.Resource?.FreeGenerator);
		}

		if (blockSpec.Propulsion?.Engine != null)
		{
			var linearEngine = blockObject.AddComponent<LinearEngine>();
			linearEngine.LoadSpec(blockSpec.Propulsion.Engine);
		}

		if (blockSpec.Propulsion?.OmniThruster != null)
		{
			var omniThruster = blockObject.AddComponent<OmniThruster>();
			omniThruster.LoadSpec(blockSpec.Propulsion.OmniThruster);
		}

		if (blockSpec.TurretedWeapon != null)
		{
			var turretedWeapon = blockObject.AddComponent<TurretedWeapon>();
			turretedWeapon.LoadSpec(blockSpec.TurretedWeapon);
		}

		var blockDescription = blockObject.AddComponent<BlockDescription>();
		blockDescription.LoadSpec(blockSpec);

		LayerUtils.SetLayerRecursively(blockObject, 6, 0b111000000);

		return blockObject;
	}
}
}