using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
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

		foreach (VehicleBlueprint.BlockInstance blockInstance in Designer.Blueprint.Blocks)
		{
			BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);
			Vector2 rootPosition = blockInstance.Position;
			Vector2 blockCenter =
				rootPosition + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, blockInstance.Rotation);

			mass += spec.Physics.Mass;
			centerOfMass += spec.Physics.Mass * blockCenter;
		}

		return mass > Mathf.Epsilon ? centerOfMass / mass : Vector2.zero;
	}
}
}