using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
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
	public VehicleDesigner Designer;
	public GameObject ControlCoreBlock;

	public int VehicleCost { get; private set; }

	private VehicleBlueprint Blueprint => Designer.Blueprint;

	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _posToBlock;
	private Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]> _blockToPos;
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _blockToObject;

	private HashSet<VehicleBlueprint.BlockInstance> _connectedBlocks;

	private void Awake()
	{
		_posToBlock = new Dictionary<Vector2Int, VehicleBlueprint.BlockInstance>();
		_blockToPos = new Dictionary<VehicleBlueprint.BlockInstance, Vector2Int[]>();
		_blockToObject = new Dictionary<VehicleBlueprint.BlockInstance, GameObject>();
		_connectedBlocks = new HashSet<VehicleBlueprint.BlockInstance>();
	}

	public void InitBlueprint()
	{
		// TODO get block name from game config
		if (Blueprint.Blocks.Count > 0)
		{
			Debug.LogError("VehicleBuilder.InitBlueprint is being used on a non-empty vehicle!");
		}

		ClearAll();
		AddBlock(BlockDatabase.Instance.GetSpecInstance("OberthEffect/ControlCore").Spec, Vector2Int.zero, 0);
	}

	private void SpawnBlockGameObject(VehicleBlueprint.BlockInstance blockInstance)
	{
		GameObject blockObject = BlockBuilder.BuildFromSpec(
			BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec, transform,
			blockInstance.Position, blockInstance.Rotation
		);

		blockObject.layer = gameObject.layer;

		BlockConfigHelper.SyncConfig(blockInstance, blockObject);

		_blockToObject.Add(blockInstance, blockObject);
	}

	public List<Vector2Int> GetConflicts(BlockSpec spec, Vector2Int rootLocation, int rotation)
	{
		return VehicleBlockUtils.AllPositionsOccupiedBy(spec, rootLocation, rotation)
			.Where(position => _posToBlock.ContainsKey(position))
			.ToList();
	}

	public void AddBlock(BlockSpec spec, Vector2Int rootPosition, int rotation)
	{
		var positions = new List<Vector2Int>();

		foreach (Vector2Int globalPosition in VehicleBlockUtils.AllPositionsOccupiedBy(spec, rootPosition, rotation))
		{
			if (_posToBlock.ContainsKey(globalPosition))
			{
				throw new DuplicateBlockError();
			}

			positions.Add(globalPosition);
		}

		var instance = new VehicleBlueprint.BlockInstance
		{
			BlockId = spec.BlockId,
			Position = rootPosition,
			Rotation = rotation
		};

		Blueprint.Blocks.Add(instance);
		foreach (Vector2Int position in positions)
		{
			_posToBlock.Add(position, instance);
		}

		_blockToPos.Add(instance, positions.ToArray());

		SpawnBlockGameObject(instance);

		UpdateConnectedBlocks();

		UpdateVehicleCost();
	}

	public void RemoveBlock(Vector2Int location)
	{
		if (!_posToBlock.TryGetValue(location, out VehicleBlueprint.BlockInstance instance))
		{
			throw new EmptyBlockError();
		}

		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(instance.BlockId).Spec;
		if (!spec.Construction.AllowErase)
		{
			throw new BlockNotErasable();
		}

		Vector2Int[] positions = _blockToPos[instance];

		foreach (Vector2Int position in positions)
		{
			_posToBlock.Remove(position);
		}

		_blockToPos.Remove(instance);

		Blueprint.Blocks.Remove(instance);

		GameObject go = _blockToObject[instance];
		Destroy(go);
		_blockToObject.Remove(instance);

		UpdateConnectedBlocks();

		UpdateVehicleCost();
	}

	#region Query Methods

	public bool HasBlockAt(Vector2Int position)
	{
		return _posToBlock.ContainsKey(position);
	}

	public VehicleBlueprint.BlockInstance GetBlockInstanceAt(Vector2Int position)
	{
		return _posToBlock.TryGetValue(position, out VehicleBlueprint.BlockInstance instance) ? instance : null;
	}

	public GameObject GetBlockObject(VehicleBlueprint.BlockInstance block)
	{
		return _blockToObject[block];
	}

	public GameObject GetBlockObjectAt(Vector2Int position)
	{
		return _posToBlock.TryGetValue(position, out VehicleBlueprint.BlockInstance instance)
			? _blockToObject[instance]
			: null;
	}

	#endregion

	#region Attachment Handling

	private void UpdateConnectedBlocks()
	{
		_connectedBlocks.Clear();
		var boundary = new Queue<VehicleBlueprint.BlockInstance>();
		VehicleBlueprint.BlockInstance controlCore = _posToBlock[Vector2Int.zero];
		boundary.Enqueue(controlCore);

		while (boundary.Count > 0)
		{
			VehicleBlueprint.BlockInstance current = boundary.Dequeue();
			if (!_connectedBlocks.Add(current)) continue;

			foreach (var target in VehicleBlockUtils.GetConnectedBlocks(current, _posToBlock))
			{
				if (_connectedBlocks.Contains(target)) continue;
				boundary.Enqueue(target);
			}
		}
	}

	public List<Vector2Int> GetDisconnectedPositions()
	{
		var disconnected = new List<Vector2Int>();

		foreach (KeyValuePair<VehicleBlueprint.BlockInstance, Vector2Int[]> pair in _blockToPos)
		{
			if (!_connectedBlocks.Contains(pair.Key))
			{
				disconnected.AddRange(pair.Value);
			}
		}

		return disconnected;
	}

	#endregion

	private void UpdateVehicleCost()
	{
		VehicleCost = VehicleHelper.GetCost(Blueprint);
		Blueprint.CachedCost = VehicleCost;
	}

	#region Administration

	private void ClearAll()
	{
		foreach (VehicleBlueprint.BlockInstance instance in _blockToObject.Keys.ToArray())
		{
			GameObject block = _blockToObject[instance];
			Destroy(block);
		}

		_posToBlock.Clear();
		_blockToPos.Clear();
		_blockToObject.Clear();
	}

	public void RenameVehicle(string vehicleName)
	{
		Blueprint.Name = vehicleName;
	}

	public void ReloadVehicle()
	{
		ClearAll();

		foreach (VehicleBlueprint.BlockInstance blockInstance in Blueprint.Blocks)
		{
			BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec;

			var positions = new List<Vector2Int>();

			foreach (Vector3Int localPosition in new BlockBounds(
				spec.Construction.BoundsMin, spec.Construction.BoundsMax
			).AllPositionsWithin)
			{
				Vector2Int globalPosition = blockInstance.Position
				                            + TransformUtils.RotatePoint(localPosition, blockInstance.Rotation);
				positions.Add(globalPosition);
				_posToBlock.Add(globalPosition, blockInstance);
			}

			_blockToPos.Add(blockInstance, positions.ToArray());

			SpawnBlockGameObject(blockInstance);
		}

		UpdateConnectedBlocks();

		UpdateVehicleCost();
	}

	#endregion
}
}