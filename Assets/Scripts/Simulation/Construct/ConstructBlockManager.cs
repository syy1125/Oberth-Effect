using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ColorScheme;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(ColorContext))]
public class ConstructBlockManager : MonoBehaviourPun, IBlockCoreRegistry, IBlockLifecycleListener,
	ICollisionRadiusProvider
{
	public GameObject DebrisPrefab;

	private bool _loaded;
	private Dictionary<Vector2Int, GameObject> _occupiedPosToBlock = new Dictionary<Vector2Int, GameObject>();
	private Dictionary<Vector2Int, GameObject> _rootPosToBlock = new Dictionary<Vector2Int, GameObject>();
	private BlockConnectivityGraph _connectivityGraph;

	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _xMin;
	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _yMin;
	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _xMax;
	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _yMax;
	private BoundsInt _bounds;
	private bool _blocksChanged;

	private struct MomentOfInertiaData
	{
		public Vector2 Position;
		public float Mass;
		public float Moment;
	}

	#region Loading

	public void LoadBlocks(ICollection<VehicleBlueprint.BlockInstance> blockInstances)
	{
		_loaded = false;

		var xMinList = new List<Tuple<VehicleBlueprint.BlockInstance, int>>(blockInstances.Count);
		var yMinList = new List<Tuple<VehicleBlueprint.BlockInstance, int>>(blockInstances.Count);
		var xMaxList = new List<Tuple<VehicleBlueprint.BlockInstance, int>>(blockInstances.Count);
		var yMaxList = new List<Tuple<VehicleBlueprint.BlockInstance, int>>(blockInstances.Count);
		_bounds = new BoundsInt();

		float totalMass = 0f;
		Vector2 centerOfMass = Vector2.zero;
		var momentOfInertiaData = new LinkedList<MomentOfInertiaData>();

		// Instantiate blocks
		foreach (VehicleBlueprint.BlockInstance blockInstance in blockInstances)
		{
			if (!BlockDatabase.Instance.HasBlock(blockInstance.BlockId))
			{
				Debug.LogError($"Failed to load block by ID: {blockInstance.BlockId}");
				continue;
			}

			BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);
			GameObject blockObject = AddBlock(
				spec, blockInstance.Position, blockInstance.Rotation,
				ref totalMass, ref centerOfMass, momentOfInertiaData
			);

			if (photonView.IsMine)
			{
				BoundsInt blockBounds = TransformUtils.TransformBounds(
					new BlockBounds(spec.Construction.BoundsMin, spec.Construction.BoundsMax).ToBoundsInt(),
					blockInstance.Position, blockInstance.Rotation
				);
				xMinList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.xMin));
				yMinList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.yMin));
				xMaxList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.xMax));
				yMaxList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.yMax));
				UnionBounds(ref _bounds, blockBounds);
			}

			BlockConfigHelper.LoadConfig(blockInstance, blockObject);
		}

		// Physics computation
		if (totalMass > Mathf.Epsilon) centerOfMass /= totalMass;
		ExportPhysicsData(totalMass, centerOfMass, momentOfInertiaData, GetComponent<Rigidbody2D>());
		transform.position -= (Vector3) centerOfMass;

		if (photonView.IsMine)
		{
			// Set up bounds queue
			xMinList.Sort((left, right) => left.Item2 - right.Item2);
			yMinList.Sort((left, right) => left.Item2 - right.Item2);
			xMaxList.Sort((left, right) => right.Item2 - left.Item2);
			yMaxList.Sort((left, right) => right.Item2 - left.Item2);
			_xMin = new Queue<Tuple<VehicleBlueprint.BlockInstance, int>>(xMinList);
			_yMin = new Queue<Tuple<VehicleBlueprint.BlockInstance, int>>(yMinList);
			_xMax = new Queue<Tuple<VehicleBlueprint.BlockInstance, int>>(xMaxList);
			_yMax = new Queue<Tuple<VehicleBlueprint.BlockInstance, int>>(yMaxList);

			// Set up connectivity graph
			_connectivityGraph = new BlockConnectivityGraph(blockInstances);
		}

		_blocksChanged = false;
		_loaded = true;
	}

	private void UnionBounds(ref BoundsInt bounds, BoundsInt add)
	{
		bounds.xMin = Mathf.Min(bounds.xMin, add.xMin);
		bounds.yMin = Mathf.Min(bounds.yMin, add.yMin);
		bounds.xMax = Mathf.Max(bounds.xMax, add.xMax);
		bounds.yMax = Mathf.Max(bounds.yMax, add.yMax);
	}

	private static void ExportPhysicsData(
		float totalMass, Vector2 centerOfMass, LinkedList<MomentOfInertiaData> momentOfInertiaData, Rigidbody2D body
	)
	{
		float momentOfInertia = momentOfInertiaData
			.Sum(blockData => blockData.Moment + blockData.Mass * (blockData.Position - centerOfMass).sqrMagnitude);

		body.mass = totalMass;
		body.centerOfMass = centerOfMass;
		body.inertia = momentOfInertia;
	}

	#endregion

	#region Block Management

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

	private GameObject AddBlock(
		BlockSpec spec, Vector2Int position, int rotation,
		ref float totalMass, ref Vector2 centerOfMass, LinkedList<MomentOfInertiaData> momentOfInertiaData
	)
	{
		GameObject blockObject = BlockBuilder.BuildFromSpec(spec, transform, position, rotation);

		Vector2 blockCenter = position + TransformUtils.RotatePoint(spec.Physics.CenterOfMass, rotation);
		totalMass += spec.Physics.Mass;
		centerOfMass += spec.Physics.Mass * blockCenter;
		momentOfInertiaData.AddLast(
			new MomentOfInertiaData
			{
				Position = blockCenter,
				Mass = spec.Physics.Mass,
				Moment = spec.Physics.MomentOfInertia
			}
		);

		_rootPosToBlock.Add(position, blockObject);
		foreach (
			Vector3Int localPosition
			in new BlockBounds(spec.Construction.BoundsMin, spec.Construction.BoundsMax).AllPositionsWithin
		)
		{
			_occupiedPosToBlock.Add(
				position + TransformUtils.RotatePoint(localPosition, rotation),
				blockObject
			);
		}

		return blockObject;
	}

	public void OnBlockDestroyedByDamage(BlockCore blockCore)
	{
		if (!photonView.IsMine) return;

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

	public void RegisterBlock(BlockCore blockCore)
	{
		// When the vehicle is loading, ignore everything as the calculation will be done by the loading routine.
		if (!_loaded) return;

		BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockCore.BlockId);
		Vector2 blockCenter = blockCore.CenterOfMassPosition;

		AddMass(blockCenter, spec.Physics.Mass, spec.Physics.MomentOfInertia);
	}

	public void UnregisterBlock(BlockCore blockCore)
	{
		if (!_loaded) return;

		BlockSpec spec = BlockDatabase.Instance.GetBlockSpec(blockCore.BlockId);
		Vector2 blockCenter = blockCore.CenterOfMassPosition;

		// Remove the block by adding negative mass
		AddMass(blockCenter, -spec.Physics.Mass, -spec.Physics.MomentOfInertia);

		if (photonView.IsMine)
		{
			_blocksChanged = true;

			if (_connectivityGraph.ContainsPosition(blockCore.RootPosition))
			{
				List<BlockConnectivityGraph> graphs =
					_connectivityGraph.SplitOnBlockDestroyed(blockCore.RootPosition);

				if (graphs == null || graphs.Count == 0)
				{
					// Construct completely destroyed. Keep the construct around so that debris can be spawned from it.
				}
				else if (graphs.Count == 1)
				{
					// Still in one piece.
					_connectivityGraph = graphs[0];
				}
				else if (graphs.Count > 1)
				{
					// Multiple chunks, create debris
					SplitChunks(graphs);
				}
				else
				{
					Debug.LogError($"Unexpected chunk count {graphs.Count}");
				}

				Debug.Log($"Destruction of block at {blockCore.RootPosition} results in {graphs?.Count} chunks.");
			}
		}
	}

	#endregion

	#region Debris

	private void SplitChunks(List<BlockConnectivityGraph> graphs)
	{
		Debug.Assert(photonView.IsMine, "photonView.IsMine");

		int primaryGraphIndex = graphs.FindIndex(graph => graph.ContainsPosition(Vector2Int.zero));
		// Having no primary graph is valid. For example, a vehicle might have its control core shot out. Or a debris might be splitting into two.
		// In that case, we still want to keep the construct around so we can extract data from it, but there is no primary graph anymore.
		_connectivityGraph = primaryGraphIndex >= 0 ? graphs[primaryGraphIndex] : BlockConnectivityGraph.Empty;

		List<Vector2Int> disableBlockRoots = new List<Vector2Int>();
		for (int i = 0; i < graphs.Count; i++)
		{
			if (i == primaryGraphIndex)
			{
				continue;
			}

			disableBlockRoots.Clear();
			disableBlockRoots.AddRange(graphs[i].AllBlocks().Select(block => block.Position));

			var debrisInfo = new DebrisInfo
			{
				OriginViewId = photonView.ViewID,
				Blocks = disableBlockRoots
					.Select(
						blockRoot => new DebrisBlockInfo
						{
							Position = blockRoot,
							DebrisState = SaveDebrisState(_rootPosToBlock[blockRoot])
						}
					)
					.ToArray()
			};

			PhotonNetwork.Instantiate(
				DebrisPrefab.name, transform.position, transform.rotation,
				0,
				new object[]
				{
					Encoding.UTF8.GetBytes(JsonUtility.ToJson(debrisInfo)),
					JsonUtility.ToJson(GetComponent<ColorContext>().ColorScheme)
				}
			);
		}
	}

	private static string SaveDebrisState(GameObject blockObject)
	{
		JObject debrisState = new JObject();

		foreach (IHasDebrisState component in blockObject.GetComponents<IHasDebrisState>())
		{
			string classKey = TypeUtils.GetClassKey(component.GetType());
			debrisState[classKey] = component.SaveDebrisState();
		}

		return debrisState.ToString(Formatting.None);
	}

	public void TransferDebrisBlocksTo(
		ConstructBlockManager receiver, IEnumerable<DebrisBlockInfo> debrisBlocks
	)
	{
		if (photonView.IsMine != receiver.photonView.IsMine)
		{
			Debug.LogWarning(
				$"Source construct IsMine={photonView.IsMine} but destination IsMine={receiver.photonView.IsMine}"
			);
		}

		receiver._loaded = false;
		float totalMass = 0f;
		Vector2 centerOfMass = Vector2.zero;
		var momentOfInertiaData = new LinkedList<MomentOfInertiaData>();

		var debrisBlockInstances = new List<VehicleBlueprint.BlockInstance>();

		foreach (DebrisBlockInfo debrisBlock in debrisBlocks)
		{
			if (!_rootPosToBlock.TryGetValue(debrisBlock.Position, out GameObject blockObject))
			{
				Debug.LogError(
					$"Tried to create debris containing block at {debrisBlock.Position} but there's no block there!"
				);
				continue;
			}

			BlockCore blockCore = blockObject.GetComponent<BlockCore>();
			BlockSpec blockSpec = BlockDatabase.Instance.GetBlockSpec(blockCore.BlockId);

			blockObject.SetActive(false);
			GameObject receiverBlock = receiver.AddBlock(
				blockSpec, blockCore.RootPosition, blockCore.Rotation,
				ref totalMass, ref centerOfMass, momentOfInertiaData
			);

			// Load debris state
			JObject debrisState = TypeUtils.ParseJson(debrisBlock.DebrisState);
			foreach (var component in receiverBlock.GetComponents<IHasDebrisState>())
			{
				string classKey = TypeUtils.GetClassKey(component.GetType());
				if (debrisState.ContainsKey(classKey))
				{
					component.LoadDebrisState(debrisState[classKey] as JObject);
				}
			}

			debrisBlockInstances.Add(
				new VehicleBlueprint.BlockInstance
				{
					BlockId = blockCore.BlockId,
					Position = blockCore.RootPosition,
					Rotation = blockCore.Rotation
				}
			);
		}

		if (totalMass > Mathf.Epsilon) centerOfMass /= totalMass;
		ExportPhysicsData(totalMass, centerOfMass, momentOfInertiaData, receiver.GetComponent<Rigidbody2D>());

		if (receiver.photonView.IsMine)
		{
			receiver._connectivityGraph = new BlockConnectivityGraph(debrisBlockInstances);
		}

		receiver._loaded = true;
	}

	#endregion

	public IEnumerable<GameObject> GetAllBlocks() => _rootPosToBlock.Values;

	public GameObject GetBlockAt(Vector2Int localPosition) =>
		_occupiedPosToBlock.TryGetValue(localPosition, out GameObject block) ? block : null;

	public BoundsInt GetBounds()
	{
		if (_blocksChanged)
		{
			PopDisabledBlocks(_xMin);
			PopDisabledBlocks(_yMin);
			PopDisabledBlocks(_xMax);
			PopDisabledBlocks(_yMax);

			if (_xMin.Count > 0 && _yMin.Count > 0 && _xMax.Count > 0 && _yMax.Count > 0)
			{
				_bounds.xMin = _xMin.Peek().Item2;
				_bounds.yMin = _yMin.Peek().Item2;
				_bounds.xMax = _xMax.Peek().Item2;
				_bounds.yMax = _yMax.Peek().Item2;
			}

			_blocksChanged = false;
		}

		return _bounds;
	}

	private void PopDisabledBlocks(Queue<Tuple<VehicleBlueprint.BlockInstance, int>> queue)
	{
		while (queue.Count > 0)
		{
			Vector2Int position = queue.Peek().Item1.Position;
			if (!_rootPosToBlock.TryGetValue(position, out GameObject value) || !value.activeSelf)
			{
				queue.Dequeue();
			}
			else
			{
				return;
			}
		}
	}

	public float GetCollisionRadius()
	{
		var bounds = GetBounds();
		return Mathf.Max(
			Mathf.Abs(bounds.xMin),
			Mathf.Abs(bounds.yMin),
			Mathf.Abs(bounds.xMax),
			Mathf.Abs(bounds.yMax)
		);
	}
}
}