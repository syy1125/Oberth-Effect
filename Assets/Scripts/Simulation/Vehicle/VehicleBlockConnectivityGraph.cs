using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
public class VehicleBlockConnectivityGraph
{
	private HashSet<VehicleBlueprint.BlockInstance> _nodes;
	private Dictionary<VehicleBlueprint.BlockInstance, HashSet<VehicleBlueprint.BlockInstance>> _edges;
	private Dictionary<Vector2Int, VehicleBlueprint.BlockInstance> _posToBlock;

	public int Count => _nodes.Count;

	private VehicleBlockConnectivityGraph()
	{}

	public VehicleBlockConnectivityGraph(ICollection<VehicleBlueprint.BlockInstance> blocks)
	{
		_nodes = new HashSet<VehicleBlueprint.BlockInstance>();
		_edges = new Dictionary<VehicleBlueprint.BlockInstance, HashSet<VehicleBlueprint.BlockInstance>>();
		_posToBlock = new Dictionary<Vector2Int, VehicleBlueprint.BlockInstance>();

		foreach (VehicleBlueprint.BlockInstance block in blocks)
		{
			_nodes.Add(block);
			var blockSpec = BlockDatabase.Instance.GetSpecInstance(block.BlockId).Spec;
			foreach (
				Vector2Int position in VehicleBlockUtils.AllPositionsOccupiedBy(
					blockSpec, block.Position, block.Rotation
				)
			)
			{
				_posToBlock.Add(position, block);
			}
		}

		foreach (VehicleBlueprint.BlockInstance block in blocks)
		{
			_edges.Add(
				block,
				new HashSet<VehicleBlueprint.BlockInstance>(VehicleBlockUtils.GetConnectedBlocks(block, _posToBlock))
			);
		}
	}

	public List<VehicleBlockConnectivityGraph> SplitOnBlockDestroyed(Vector2Int position)
	{
		return SplitOnBlockDestroyed(_posToBlock[position]);
	}

	public List<VehicleBlockConnectivityGraph> SplitOnBlockDestroyed(VehicleBlueprint.BlockInstance block)
	{
		List<VehicleBlueprint.BlockInstance> unexploredRoots = new List<VehicleBlueprint.BlockInstance>(_edges[block]);

		RemoveNode(block);

		if (unexploredRoots.Count == 0)
		{
			// The craft is obliterated so utterly that there's nothing left?
			return null;
		}

		var root = unexploredRoots[0];
		unexploredRoots.RemoveAt(0);
		var chunk = FloodFill(root, unexploredRoots, true);

		if (unexploredRoots.Count == 0)
		{
			// Vehicle still fully connected, there's only one chunk and it contains all the nodes
			return new List<VehicleBlockConnectivityGraph> { this };
		}
		else
		{
			List<HashSet<VehicleBlueprint.BlockInstance>> chunks = new List<HashSet<VehicleBlueprint.BlockInstance>>();
			chunks.Add(chunk);

			while (unexploredRoots.Count > 0)
			{
				root = unexploredRoots[0];
				unexploredRoots.RemoveAt(0);
				chunk = FloodFill(root, unexploredRoots);
				chunks.Add(chunk);
			}

			List<VehicleBlockConnectivityGraph> output = new List<VehicleBlockConnectivityGraph> { this };

			for (int i = 1; i < chunks.Count; i++)
			{
				var splitGraph = new VehicleBlockConnectivityGraph
				{
					_nodes = chunks[i],
					_edges = new Dictionary<VehicleBlueprint.BlockInstance, HashSet<VehicleBlueprint.BlockInstance>>(),
					_posToBlock = new Dictionary<Vector2Int, VehicleBlueprint.BlockInstance>()
				};

				_nodes.ExceptWith(chunks[i]);
				foreach (VehicleBlueprint.BlockInstance chunkBlock in chunks[i])
				{
					splitGraph._edges.Add(chunkBlock, _edges[chunkBlock]);
					_edges.Remove(chunkBlock);

					var blockSpec = BlockDatabase.Instance.GetSpecInstance(chunkBlock.BlockId).Spec;
					foreach (
						var position in VehicleBlockUtils.AllPositionsOccupiedBy(
							blockSpec, chunkBlock.Position, chunkBlock.Rotation
						)
					)
					{
						splitGraph._posToBlock.Add(position, chunkBlock);
						_posToBlock.Remove(position);
					}
				}

				output.Add(splitGraph);
			}

			return output;
		}
	}

	private void RemoveNode(VehicleBlueprint.BlockInstance block)
	{
		_nodes.Remove(block);
		foreach (VehicleBlueprint.BlockInstance other in _edges[block])
		{
			_edges[other].Remove(block);
		}

		_edges.Remove(block);

		var blockSpec = BlockDatabase.Instance.GetSpecInstance(block.BlockId).Spec;
		foreach (
			Vector2Int position in VehicleBlockUtils.AllPositionsOccupiedBy(blockSpec, block.Position, block.Rotation)
		)
		{
			_posToBlock.Remove(position);
		}
	}

	private HashSet<VehicleBlueprint.BlockInstance> FloodFill(
		VehicleBlueprint.BlockInstance root, List<VehicleBlueprint.BlockInstance> unexploredRoots,
		bool firstPass = false
	)
	{
		HashSet<VehicleBlueprint.BlockInstance> connectedBlocks = new HashSet<VehicleBlueprint.BlockInstance>();
		Queue<VehicleBlueprint.BlockInstance> boundary = new Queue<VehicleBlueprint.BlockInstance>();
		boundary.Enqueue(root);

		while (boundary.Count > 0)
		{
			VehicleBlueprint.BlockInstance current = boundary.Dequeue();
			if (!connectedBlocks.Add(current)) continue;

			foreach (VehicleBlueprint.BlockInstance target in _edges[current])
			{
				if (connectedBlocks.Contains(target)) continue;
				boundary.Enqueue(target);

				if (unexploredRoots.Remove(target))
				{
					// On the first pass, if unexplored roots is empty, that means the vehicle is still fully connected, because from one root you are able to reach all the other roots.
					if (unexploredRoots.Count == 0 && firstPass) return null;
				}
			}
		}

		return connectedBlocks;
	}

	public IEnumerable<VehicleBlueprint.BlockInstance> AllBlocks()
	{
		return _nodes;
	}

	public bool ContainsPosition(Vector2Int position)
	{
		return _posToBlock.ContainsKey(position);
	}
}
}