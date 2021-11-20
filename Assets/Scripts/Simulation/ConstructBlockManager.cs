using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Simulation.Vehicle;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class ConstructBlockManager : MonoBehaviourPun, IBlockCoreRegistry, IBlockLifecycleListener
{
	private bool _loaded;
	private Dictionary<Vector2Int, GameObject> _occupiedPosToBlock;
	private Dictionary<Vector2Int, GameObject> _rootPosToBlock;
	private VehicleBlockConnectivityGraph _connectivityGraph;

	private void Awake()
	{
		_occupiedPosToBlock = new Dictionary<Vector2Int, GameObject>();
		_rootPosToBlock = new Dictionary<Vector2Int, GameObject>();
	}

	public void LoadVehicle(ICollection<VehicleBlueprint.BlockInstance> blockInstances)
	{
		float totalMass = 0f;
		Vector2 centerOfMass = Vector2.zero;
		var momentOfInertiaData = new LinkedList<Tuple<Vector2, float, float>>();

		// Instantiate blocks
		foreach (VehicleBlueprint.BlockInstance blockInstance in blockInstances)
		{
			if (!BlockDatabase.Instance.HasBlock(blockInstance.BlockId))
			{
				Debug.LogError($"Failed to load block by ID: {blockInstance.BlockId}");
				continue;
			}

			BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);

			GameObject blockObject = BlockBuilder.BuildFromSpec(
				spec, transform, blockInstance.Position, blockInstance.Rotation
			);
			Vector2 blockCenter = blockInstance.Position
			                      + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, blockInstance.Rotation);
			totalMass += spec.Physics.Mass;
			centerOfMass += spec.Physics.Mass * blockCenter;
			momentOfInertiaData.AddLast(
				new Tuple<Vector2, float, float>(blockCenter, spec.Physics.Mass, spec.Physics.MomentOfInertia)
			);

			_rootPosToBlock.Add(blockInstance.Position, blockObject);
			foreach (
				Vector3Int localPosition
				in new BlockBounds(spec.Construction.BoundsMin, spec.Construction.BoundsMax).AllPositionsWithin
			)
			{
				_occupiedPosToBlock.Add(
					blockInstance.Position + TransformUtils.RotatePoint(localPosition, blockInstance.Rotation),
					blockObject
				);
			}
		}

		// Physics computation
		if (totalMass > Mathf.Epsilon)
		{
			centerOfMass /= totalMass;
		}

		var momentOfInertia = 0f;
		foreach (Tuple<Vector2, float, float> blockData in momentOfInertiaData)
		{
			(Vector2 position, float mass, float blockMoment) = blockData;
			momentOfInertia += blockMoment + mass * (position - centerOfMass).sqrMagnitude;
		}

		var body = GetComponent<Rigidbody2D>();
		body.mass = totalMass;
		body.centerOfMass = centerOfMass;
		body.inertia = momentOfInertia;

		transform.position -= (Vector3) centerOfMass;

		// Set up connectivity graph
		_connectivityGraph = new VehicleBlockConnectivityGraph(blockInstances);

		// Load config
		foreach (VehicleBlueprint.BlockInstance blockInstance in blockInstances)
		{
			BlockConfigHelper.LoadConfig(blockInstance, _rootPosToBlock[blockInstance.Position]);
		}

		_loaded = true;
	}

	public void RegisterBlock(BlockCore blockCore)
	{
		// When the vehicle is loading, ignore everything as the calculation will be done by the loading routine.
		if (!_loaded) return;
		if (!photonView.IsMine) return;

		BlockCore core = blockCore.GetComponent<BlockCore>();
		BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(core.BlockId);
		Vector2 blockCenter = blockCore.CenterOfMassPosition;

		AddMass(blockCenter, spec.Physics.Mass, spec.Physics.MomentOfInertia);

		var body = GetComponent<Rigidbody2D>();
		photonView.RPC(
			nameof(UpdateRigidbody2D), RpcTarget.OthersBuffered, body.mass, body.centerOfMass, body.inertia
		);
	}

	public void UnregisterBlock(BlockCore blockCore)
	{
		if (!_loaded) return;
		if (!photonView.IsMine) return;

		BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockCore.BlockId);
		Vector2 blockCenter = blockCore.CenterOfMassPosition;

		// Remove the block by adding negative mass
		AddMass(blockCenter, -spec.Physics.Mass, -spec.Physics.MomentOfInertia);

		if (_connectivityGraph.ContainsPosition(blockCore.RootPosition))
		{
			List<VehicleBlockConnectivityGraph> graphs =
				_connectivityGraph.SplitOnBlockDestroyed(blockCore.RootPosition);

			if (graphs == null || graphs.Count == 0)
			{
				// Vehicle completely destroyed. Probably nothing need to be done here?
			}
			else if (graphs.Count == 1)
			{
				_connectivityGraph = graphs[0];
			}
			else if (graphs.Count > 1)
			{
				int primaryGraphIndex = graphs.FindIndex(graph => graph.ContainsPosition(Vector2Int.zero));

				List<Vector2Int> disableRoots = new List<Vector2Int>();
				for (int i = 0; i < graphs.Count; i++)
				{
					if (i == primaryGraphIndex) continue;

					disableRoots.Clear();
					disableRoots.AddRange(graphs[i].AllBlocks().Select(block => block.Position));

					photonView.RPC(
						nameof(DisableBlocks), RpcTarget.AllBuffered,
						disableRoots.Select(position => position.x).ToArray(),
						disableRoots.Select(position => position.y).ToArray()
					);
				}

				if (primaryGraphIndex >= 0)
				{
					_connectivityGraph = graphs[primaryGraphIndex];
				}
			}
			else
			{
				Debug.LogError($"Unexpected chunk count {graphs.Count}");
			}

			Debug.Log($"Destruction of block at {blockCore.RootPosition} results in {graphs?.Count} chunks.");
		}

		var body = GetComponent<Rigidbody2D>();
		photonView.RPC(
			nameof(UpdateRigidbody2D), RpcTarget.OthersBuffered, body.mass, body.centerOfMass, body.inertia
		);
	}

	private void AddMass(Vector2 position, float mass, float moment)
	{
		var body = GetComponent<Rigidbody2D>();

		float totalMass = body.mass + mass;
		Vector2 centerOfMass = totalMass > Mathf.Epsilon
			? (body.centerOfMass * body.mass + position * mass) / totalMass
			: Vector2.zero;
		float momentOfInertia = body.inertia
		                        + body.mass * (body.centerOfMass - centerOfMass).sqrMagnitude
		                        + moment
		                        + mass * (position - centerOfMass).sqrMagnitude;

		body.mass = totalMass;
		body.centerOfMass = centerOfMass;
		body.inertia = momentOfInertia;
	}

	public void OnBlockDestroyedByDamage(BlockCore blockCore)
	{
		photonView.RPC(
			nameof(DisableBlocks), RpcTarget.AllBuffered,
			new[] { blockCore.RootPosition.x }, new[] { blockCore.RootPosition.y }
		);
	}

	// Photon can't serialize Vector2Int
	[PunRPC]
	private void DisableBlocks(int[] x, int[] y)
	{
		for (int i = 0; i < x.Length; i++)
		{
			_rootPosToBlock[new Vector2Int(x[i], y[i])].SetActive(false);
		}
	}

	[PunRPC]
	private void UpdateRigidbody2D(float mass, Vector2 centerOfMass, float momentOfInertia)
	{
		var body = GetComponent<Rigidbody2D>();
		body.mass = mass;
		body.centerOfMass = centerOfMass;
		body.inertia = momentOfInertia;
	}

	public IEnumerable<GameObject> GetAllBlocks() => _rootPosToBlock.Values;

	public GameObject GetBlockAt(Vector2Int localPosition) =>
		_occupiedPosToBlock.TryGetValue(localPosition, out GameObject block) ? block : null;
}
}