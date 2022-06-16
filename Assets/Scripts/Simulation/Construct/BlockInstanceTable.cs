using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
public class BlockInstanceTable
{
	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _rootPosIndex = new();
	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _posIndex = new();
	private Dictionary<VehicleBlueprint.BlockInstance, GameObject> _instanceToBlock = new();

	public void Add(VehicleBlueprint.BlockInstance blockInstance, GameObject blockObject, BlockBounds bounds)
	{
		_rootPosIndex.Add(blockInstance.Position, blockInstance);
		_instanceToBlock.Add(blockInstance, blockObject);

		foreach (Vector3Int localPosition in bounds.AllPositionsWithin)
		{
			_posIndex.Add(new Vector2Int(localPosition.x, localPosition.y), blockInstance);
		}
	}

	public VehicleBlueprint.BlockInstance GetInstanceWithRoot(Vector2Int root)
	{
		return _rootPosIndex.TryGetValue(root, out VehicleBlueprint.BlockInstance instance) ? instance : null;
	}

	public VehicleBlueprint.BlockInstance GetInstanceOccupying(Vector2Int pos)
	{
		return _posIndex.TryGetValue(pos, out VehicleBlueprint.BlockInstance instance) ? instance : null;
	}

	public GameObject GetObjectWithRoot(Vector2Int root)
	{
		if (_rootPosIndex.TryGetValue(root, out VehicleBlueprint.BlockInstance instance))
		{
			return _instanceToBlock[instance];
		}
		else
		{
			return null;
		}
	}

	public GameObject GetObjectOccupying(Vector2Int pos)
	{
		if (_posIndex.TryGetValue(pos, out VehicleBlueprint.BlockInstance instance))
		{
			return _instanceToBlock[instance];
		}
		else
		{
			return null;
		}
	}

	public GameObject GetObject(VehicleBlueprint.BlockInstance instance)
	{
		return _instanceToBlock.TryGetValue(instance, out GameObject blockObject) ? blockObject : null;
	}

	public IEnumerable<GameObject> GetAllObjects() => _instanceToBlock.Values;
}
}