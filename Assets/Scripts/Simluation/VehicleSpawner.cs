using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
	public void SpawnVehicle(VehicleBlueprint blueprint)
	{
		foreach (VehicleBlueprint.BlockInstance block in blueprint.Blocks)
		{
			GameObject blockPrefab = BlockRegistry.Instance.GetBlock(block.BlockID);
			
			if (blockPrefab == null)
			{
				Debug.LogError($"Failed to load block by ID: {block.BlockID}");
				continue;
			}

			GameObject go = Instantiate(blockPrefab, transform);
			go.transform.localPosition = new Vector3(block.X, block.Y);
			go.transform.localRotation = Quaternion.AngleAxis(block.Rotation * 90f, Vector3.forward);
		}
	}
}
