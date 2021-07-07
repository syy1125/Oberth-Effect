using System;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Designer.Config;
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

	public void InitVehicle()
	{
		AddBlock(ControlCoreBlock, Vector2Int.zero, 0);
	}

	private void SpawnBlockGameObject(VehicleBlueprint.BlockInstance instance, GameObject blockPrefab)
	{
		GameObject go = Instantiate(blockPrefab, transform);

		go.transform.localPosition = new Vector3(instance.X, instance.Y);
		go.transform.localRotation = RotationUtils.GetPhysicalRotation(instance.Rotation);
		go.layer = gameObject.layer;

		DesignerConfig.SyncConfig(go, instance);

		_blockToObject.Add(instance, go);
	}

	private static IEnumerable<Vector2Int> AllPositionsOccupiedBy(BlockInfo info, Vector2Int rootLocation, int rotation)
	{
		foreach (Vector3Int localPosition in info.Bounds.allPositionsWithin)
		{
			yield return rootLocation + RotationUtils.RotatePoint(localPosition, rotation);
		}
	}

	public List<Vector2Int> GetConflicts(GameObject blockPrefab, Vector2Int rootLocation, int rotation)
	{
		var info = blockPrefab.GetComponent<BlockInfo>();

		return AllPositionsOccupiedBy(info, rootLocation, rotation)
			.Where(position => _posToBlock.ContainsKey(position))
			.ToList();
	}

	public void AddBlock(GameObject blockPrefab, Vector2Int rootLocation, int rotation)
	{
		var info = blockPrefab.GetComponent<BlockInfo>();

		var positions = new List<Vector2Int>();

		foreach (Vector2Int globalPosition in AllPositionsOccupiedBy(info, rootLocation, rotation))
		{
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

		Blueprint.Blocks.Add(instance);
		foreach (Vector2Int position in positions)
		{
			_posToBlock.Add(position, instance);
		}

		_blockToPos.Add(instance, positions.ToArray());

		SpawnBlockGameObject(instance, blockPrefab);

		UpdateConnectedBlocks();
	}

	public void RemoveBlock(Vector2Int location)
	{
		if (!_posToBlock.TryGetValue(location, out VehicleBlueprint.BlockInstance instance))
		{
			throw new EmptyBlockError();
		}

		GameObject blockTemplate = BlockDatabase.Instance.GetBlock(instance.BlockID);
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

		Blueprint.Blocks.Remove(instance);

		GameObject go = _blockToObject[instance];
		Destroy(go);
		_blockToObject.Remove(instance);

		UpdateConnectedBlocks();
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

	private static IEnumerable<Vector2Int> AttachmentPoints(VehicleBlueprint.BlockInstance instance)
	{
		GameObject blockPrefab = BlockDatabase.Instance.GetBlock(instance.BlockID);
		foreach (Vector2Int attachmentPoint in blockPrefab.GetComponent<BlockInfo>().AttachmentPoints)
		{
			yield return new Vector2Int(instance.X, instance.Y)
			             + RotationUtils.RotatePoint(attachmentPoint, instance.Rotation);
		}
	}

	private void UpdateConnectedBlocks()
	{
		_connectedBlocks.Clear();
		var boundary = new LinkedList<VehicleBlueprint.BlockInstance>();

		void Expand(VehicleBlueprint.BlockInstance instance)
		{
			if (_connectedBlocks.Contains(instance)) return;

			_connectedBlocks.Add(instance);

			foreach (Vector2Int attachmentPoint in AttachmentPoints(instance))
			{
				if (_posToBlock.TryGetValue(attachmentPoint, out VehicleBlueprint.BlockInstance adjacentInstance))
				{
					if (_connectedBlocks.Contains(adjacentInstance)) continue;


					if (IsConnected(adjacentInstance, instance))
					{
						boundary.AddLast(adjacentInstance);
					}
				}
			}
		}

		// A block is connected if it "connects back" to one of the originator's positions
		bool IsConnected(VehicleBlueprint.BlockInstance fromInstance, VehicleBlueprint.BlockInstance toInstance)
		{
			foreach (Vector2Int attachmentPoint in AttachmentPoints(fromInstance))
			{
				if (
					_posToBlock.TryGetValue(attachmentPoint, out VehicleBlueprint.BlockInstance targetInstance)
					&& targetInstance == toInstance
				)
				{
					return true;
				}
			}

			return false;
		}

		VehicleBlueprint.BlockInstance controlCore = _posToBlock[Vector2Int.zero];
		Expand(controlCore);

		while (boundary.Count > 0)
		{
			VehicleBlueprint.BlockInstance next = boundary.First.Value;
			boundary.RemoveFirst();
			Expand(next);
		}
	}

	public IEnumerable<Vector2Int> GetDisconnectedPositions()
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

		foreach (VehicleBlueprint.BlockInstance instance in Blueprint.Blocks)
		{
			GameObject blockPrefab = BlockDatabase.Instance.GetBlock(instance.BlockID);

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

		UpdateConnectedBlocks();
	}
}
}