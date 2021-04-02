using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VehicleSpawner : MonoBehaviour
{
	public void SpawnVehicle(VehicleBlueprint blueprint)
	{
		float totalMass = 0f;
		Vector2 centerOfMass = Vector2.zero;
		var momentOfInertiaData = new LinkedList<Tuple<Vector2, float, float>>();

		foreach (VehicleBlueprint.BlockInstance block in blueprint.Blocks)
		{
			GameObject blockPrefab = BlockRegistry.Instance.GetBlock(block.BlockID);

			if (blockPrefab == null)
			{
				Debug.LogError($"Failed to load block by ID: {block.BlockID}");
				continue;
			}

			var rootLocation = new Vector2(block.X, block.Y);

			GameObject go = Instantiate(blockPrefab, transform);
			go.transform.localPosition = rootLocation;
			go.transform.localRotation = RotationUtils.GetPhysicalRotation(block.Rotation);

			var info = blockPrefab.GetComponent<BlockInfo>();

			Vector2 blockCenter = rootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);
			totalMass += info.Mass;
			centerOfMass += info.Mass * blockCenter;
			momentOfInertiaData.AddLast(new Tuple<Vector2, float, float>(blockCenter, info.Mass, info.MomentOfInertia));
		}

		if (totalMass > Mathf.Epsilon)
		{
			centerOfMass /= totalMass;
		}

		float momentOfInertia = 0f;
		foreach (Tuple<Vector2, float, float> blockData in momentOfInertiaData)
		{
			(Vector2 position, float mass, float blockMoment) = blockData;
			momentOfInertia += blockMoment + mass * (position - centerOfMass).sqrMagnitude;
		}

		var body = GetComponent<Rigidbody2D>();
		body.mass = totalMass;
		body.centerOfMass = centerOfMass;
		body.inertia = momentOfInertia;
	}
}