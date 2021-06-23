using System;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehiclePhysicsControl : MonoBehaviour, IBlockCoreRegistry
{
	private Rigidbody2D _body;
	private Rigidbody2D Body => _body == null ? _body = GetComponent<Rigidbody2D>() : _body;

	public void RegisterBlock(BlockCore block)
	{
		var info = block.GetComponent<BlockInfo>();
		Vector2 blockCenterOfMass = block.RootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);
		AddBlockMass(blockCenterOfMass, info.Mass, info.MomentOfInertia);
	}

	public void UnregisterBlock(BlockCore block)
	{
		var info = block.GetComponent<BlockInfo>();
		Vector2 blockCenterOfMass = block.RootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);
		// "Subtract" the block by adding negative mass
		AddBlockMass(blockCenterOfMass, -info.Mass, -info.MomentOfInertia);
	}

	private void AddBlockMass(Vector2 blockCenterOfMass, float blockMass, float blockMoment)
	{
		float totalMass = Body.mass + blockMass;
		Vector2 centerOfMass = totalMass > Mathf.Epsilon
			? (Body.centerOfMass * Body.mass + blockCenterOfMass * blockMass) / totalMass
			: Vector2.zero;
		float momentOfInertia = Body.inertia
		                        + Body.mass * (Body.centerOfMass - centerOfMass).sqrMagnitude
		                        + blockMoment
		                        + blockMass * (blockCenterOfMass - centerOfMass).sqrMagnitude;

		Body.mass = totalMass;
		Body.centerOfMass = centerOfMass;
		Body.inertia = momentOfInertia;
	}
}
}