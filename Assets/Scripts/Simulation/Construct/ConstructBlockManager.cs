using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Blocks.Config;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Physics;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(ColorContext))]
public class ConstructBlockManager : MonoBehaviourPun,
	IBlockCoreRegistry,
	IBlockLifecycleListener,
	ICollisionRadiusProvider,
	IPunObservable
{
	public string DebrisPrefabName = "Debris";

	private bool _loaded;

	private BlockInstanceTable _blockTable = new();

	private BlockConnectivityGraph _connectivityGraph;

	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _xMin;
	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _yMin;
	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _xMax;
	private Queue<Tuple<VehicleBlueprint.BlockInstance, int>> _yMax;
	private BoundsInt _bounds;
	private float _collisionRadius;
	private bool _blocksChanged;

	private struct MomentOfInertiaData
	{
		public Vector2 Position;
		public float Mass;
		public float Moment;
	}

	#region Loading

	public void LoadBlocks(IList<VehicleBlueprint.BlockInstance> blockInstances, BlockContext context)
	{
		LoadBlocks(blockInstances, context, null);
	}

	private void LoadBlocks(
		IList<VehicleBlueprint.BlockInstance> blockInstances, BlockContext context, Action<int, GameObject> postAction
	)
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
		for (var index = 0; index < blockInstances.Count; index++)
		{
			VehicleBlueprint.BlockInstance blockInstance = blockInstances[index];
			if (!BlockDatabase.Instance.ContainsId(blockInstance.BlockId))
			{
				Debug.LogError($"Failed to load block by ID: {blockInstance.BlockId}");
				continue;
			}

			BlockSpec blockSpec = BlockDatabase.Instance.GetBlockSpec(blockInstance.BlockId);
			BlockBounds blockBounds =
				new BlockBounds(blockSpec.Construction.BoundsMin, blockSpec.Construction.BoundsMax)
					.Transformed(blockInstance.Position, blockInstance.Rotation);
			GameObject blockObject = InstantiateBlock(
				blockSpec, blockInstance.Position, blockInstance.Rotation, context,
				ref totalMass, ref centerOfMass, momentOfInertiaData
			);
			BlockConfigHelper.LoadConfig(blockInstance, blockObject);

			postAction?.Invoke(index, blockObject);
			_blockTable.Add(blockInstance, blockObject, blockBounds);

			if (photonView.IsMine)
			{
				xMinList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.Min.x));
				yMinList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.Min.y));
				xMaxList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.Max.x));
				yMaxList.Add(new Tuple<VehicleBlueprint.BlockInstance, int>(blockInstance, blockBounds.Max.y));
				UnionBounds(ref _bounds, blockBounds.ToBoundsInt());
			}
		}

		float xDist = Mathf.Max(Mathf.Abs(_bounds.xMin), Mathf.Abs(_bounds.xMax));
		float yDist = Mathf.Max(Mathf.Abs(_bounds.yMin), Mathf.Abs(_bounds.yMax));
		_collisionRadius = Mathf.Sqrt(xDist * xDist + yDist * yDist);

		// Physics computation
		if (totalMass > Mathf.Epsilon) centerOfMass /= totalMass;
		ExportPhysicsData(totalMass, centerOfMass, momentOfInertiaData, GetComponent<Rigidbody2D>());

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

	private GameObject InstantiateBlock(
		BlockSpec spec, Vector2Int position, int rotation, in BlockContext context,
		ref float totalMass, ref Vector2 centerOfMass, LinkedList<MomentOfInertiaData> momentOfInertiaData
	)
	{
		GameObject blockObject = BlockBuilder.BuildFromSpec(spec, transform, position, rotation, context);

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

		return blockObject;
	}

	public void OnBlockDestroyedByDamage(BlockCore blockCore)
	{
		if (!photonView.IsMine) return;

		photonView.RPC(
			nameof(DisableBlockAt), RpcTarget.AllBuffered,
			blockCore.RootPosition.x, blockCore.RootPosition.y
		);
	}

	// Photon can't serialize Vector2Int
	[PunRPC]
	private void DisableBlockAt(int x, int y)
	{
		_blockTable.GetObjectWithRoot(new Vector2Int(x, y)).SetActive(false);
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

		for (int i = 0; i < graphs.Count; i++)
		{
			if (i == primaryGraphIndex)
			{
				continue;
			}

			var debrisInfo = new DebrisInfo
			{
				OriginViewId = photonView.ViewID,
				Blocks = graphs[i]
					.AllBlocks()
					.Select(
						blockInstance => new DebrisBlockInfo
						{
							Position = blockInstance.Position,
							DebrisState = SaveDebrisState(_blockTable.GetObject(blockInstance))
						}
					)
					.ToArray()
			};

			PhotonNetwork.Instantiate(
				DebrisPrefabName, transform.position, transform.rotation,
				0,
				new object[]
				{
					CompressionUtils.Compress(JsonUtility.ToJson(debrisInfo)),
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

	private static void LoadDebrisState(GameObject blockObject, string stateString)
	{
		JObject debrisState = TypeUtils.ParseJson(stateString);

		foreach (var component in blockObject.GetComponents<IHasDebrisState>())
		{
			string classKey = TypeUtils.GetClassKey(component.GetType());
			if (debrisState.ContainsKey(classKey))
			{
				component.LoadDebrisState(debrisState[classKey] as JObject);
			}
		}
	}

	public void TransferDebrisBlocksTo(
		ConstructBlockManager receiver, IList<DebrisBlockInfo> debrisBlocks
	)
	{
		if (photonView.IsMine != receiver.photonView.IsMine)
		{
			Debug.LogWarning(
				$"Source construct IsMine={photonView.IsMine} but destination IsMine={receiver.photonView.IsMine}"
			);
		}

		foreach (DebrisBlockInfo info in debrisBlocks)
		{
			GameObject blockObject = _blockTable.GetObjectWithRoot(info.Position);

			if (blockObject == null)
			{
				Debug.LogError(
					$"Tried to create debris containing block at {info.Position} but there's no block there!"
				);
				continue;
			}

			blockObject.SetActive(false);
		}

		List<VehicleBlueprint.BlockInstance> blockInstances =
			debrisBlocks
				.Select(item => _blockTable.GetInstanceWithRoot(item.Position))
				.ToList();

		receiver.LoadBlocks(
			blockInstances,
			new BlockContext { IsMainVehicle = false },
			(index, blockObject) => LoadDebrisState(blockObject, debrisBlocks[index].DebrisState)
		);
	}

	#endregion

	#region Unity Callbacks

	private void Update()
	{
		if (photonView.IsMine && _blocksChanged)
		{
			UpdateBounds();
			_blocksChanged = false;
		}
	}

	private void UpdateBounds()
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

		float xDist = Mathf.Max(Mathf.Abs(_bounds.xMin), Mathf.Abs(_bounds.xMax));
		float yDist = Mathf.Max(Mathf.Abs(_bounds.yMin), Mathf.Abs(_bounds.yMax));
		_collisionRadius = Mathf.Sqrt(xDist * xDist + yDist * yDist);
	}

	private void PopDisabledBlocks(Queue<Tuple<VehicleBlueprint.BlockInstance, int>> queue)
	{
		while (queue.Count > 0)
		{
			GameObject blockObject = _blockTable.GetObject(queue.Peek().Item1);
			if (blockObject == null || !blockObject.activeSelf)
			{
				queue.Dequeue();
			}
			else
			{
				return;
			}
		}
	}

	#endregion

	public IEnumerable<GameObject> GetAllBlocks() => _blockTable.GetAllObjects();

	public GameObject GetBlockOccupying(Vector2Int position)
	{
		return _blockTable.GetObjectOccupying(position);
	}

	public BoundsInt GetBounds()
	{
		return _bounds;
	}

	public float GetCollisionRadius()
	{
		return _collisionRadius;
	}

	public float GetCollisionSqrRadius()
	{
		return _collisionRadius * _collisionRadius;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_collisionRadius);
			stream.SendNext(_bounds.xMin);
			stream.SendNext(_bounds.yMin);
			stream.SendNext(_bounds.xMax);
			stream.SendNext(_bounds.yMax);
		}
		else
		{
			_collisionRadius = (float) stream.ReceiveNext();
			int xMin = (int) stream.ReceiveNext();
			int yMin = (int) stream.ReceiveNext();
			int xMax = (int) stream.ReceiveNext();
			int yMax = (int) stream.ReceiveNext();
			_bounds.SetMinMax(new Vector3Int(xMin, yMin, 0), new Vector3Int(xMax, yMax, 1));
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.DrawWireSphere(transform.position, GetCollisionRadius());
	}
}
}