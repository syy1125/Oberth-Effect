using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerCenterOfMassContext : CenterOfMassContext
{
	public VehicleDesigner Designer;
	public VehicleBuilder Builder;

	public override Vector2 GetCenterOfMass()
	{
		Vector2 centerOfMass = Vector2.zero;
		float mass = 0f;

		foreach (VehicleBlueprint.BlockInstance block in Designer.Blueprint.Blocks)
		{
			GameObject blockObject = Builder.GetBlockObject(block);
			Vector2 rootLocation = new Vector2(block.X, block.Y);
			BlockInfo info = blockObject.GetComponent<BlockInfo>();
			Vector2 blockCenter = rootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);

			mass += info.Mass;
			centerOfMass += info.Mass * blockCenter;
		}

		return mass > Mathf.Epsilon ? centerOfMass / mass : Vector2.zero;
	}
}
}