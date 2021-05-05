using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Vehicle;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
internal class DuplicateBlockError : Exception
{}

internal class EmptyBlockError : Exception
{}

internal class BlockNotErasable : Exception
{}

// Designer component responsible for interacting with the blueprint
public class VehicleBuilder : MonoBehaviour
{
	private VehicleBlueprint _blueprint;
	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _posToBlock;
	private Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]> _blockToPos;
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _blockToObject;

	private void Awake()
	{
		_blueprint = new VehicleBlueprint();
		_posToBlock = new Dictionary<Vector2Int, VehicleBlueprint.BlockInstance>();
		_blockToPos = new Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]>();
		_blockToObject = new Dictionary<VehicleBlueprint.BlockInstance, GameObject>();
	}

	private void SpawnBlockGameObject(VehicleBlueprint.BlockInstance instance, GameObject blockPrefab)
	{
		GameObject go = Instantiate(blockPrefab, transform);
		go.transform.localPosition = new Vector3(instance.X, instance.Y);
		go.transform.localRotation = RotationUtils.GetPhysicalRotation(instance.Rotation);
		go.layer = gameObject.layer;

		_blockToObject.Add(instance, go);
	}

	public void AddBlock(GameObject blockPrefab, Vector2Int rootLocation, int rotation)
	{
		var info = blockPrefab.GetComponent<BlockInfo>();

		var positions = new List<Vector2Int>();

		foreach (Vector3Int localPosition in info.Bounds.allPositionsWithin)
		{
			Vector2Int globalPosition = rootLocation + RotationUtils.RotatePoint(localPosition, rotation);

			if (_posToBlock.ContainsKey(globalPosition))
			{
				throw new DuplicateBlockError();
			}

			positions.Add(globalPosition);
		}

		var instance = new VehicleBlueprint.BlockInstance
		{
			BlockID = info.BlockID,
			X = rootLocation.x,
			Y = rootLocation.y,
			Rotation = rotation
		};

		_blueprint.Blocks.Add(instance);
		foreach (Vector2Int position in positions)
		{
			_posToBlock.Add(position, instance);
		}

		_blockToPos.Add(instance, positions.ToArray());

		SpawnBlockGameObject(instance, blockPrefab);
	}

	public void RemoveBlock(Vector2Int location)
	{
		if (!_posToBlock.TryGetValue(location, out VehicleBlueprint.BlockInstance instance))
		{
			throw new EmptyBlockError();
		}

		GameObject blockTemplate = BlockRegistry.Instance.GetBlock(instance.BlockID);
		if (!blockTemplate.GetComponent<BlockInfo>().AllowErase)
		{
			throw new BlockNotErasable();
		}

		Vector2Int[] positions = _blockToPos[instance];

		foreach (Vector2Int position in positions)
		{
			_posToBlock.Remove(position);
		}

		_blockToPos.Remove(instance);

		_blueprint.Blocks.Remove(instance);

		GameObject go = _blockToObject[instance];
		Destroy(go);
		_blockToObject.Remove(instance);
	}

	private void ClearAll()
	{
		foreach (VehicleBlueprint.BlockInstance instance in _blueprint.Blocks)
		{
			GameObject block = _blockToObject[instance];
			Destroy(block);
		}

		_blueprint = new VehicleBlueprint();
		_posToBlock.Clear();
		_blockToPos.Clear();
		_blockToObject.Clear();
	}

	public void RenameVehicle(string vehicleName)
	{
		_blueprint.Name = vehicleName;
	}

	public string SaveVehicle()
	{
		return JsonUtility.ToJson(_blueprint);
	}

	public void LoadVehicle(string blueprint)
	{
		ClearAll();

		_blueprint = JsonUtility.FromJson<VehicleBlueprint>(blueprint);

		foreach (VehicleBlueprint.BlockInstance instance in _blueprint.Blocks)
		{
			GameObject blockPrefab = BlockRegistry.Instance.GetBlock(instance.BlockID);

			var info = blockPrefab.GetComponent<BlockInfo>();

			var positions = new List<Vector2Int>();

			foreach (Vector3Int localPosition in info.Bounds.allPositionsWithin)
			{
				Vector2Int globalPosition = new Vector2Int(instance.X, instance.Y)
				                            + RotationUtils.RotatePoint(localPosition, instance.Rotation);
				positions.Add(globalPosition);
				_posToBlock.Add(globalPosition, instance);
			}

			_blockToPos.Add(instance, positions.ToArray());

			SpawnBlockGameObject(instance, blockPrefab);
		}
	}
}
}