using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using UnityEngine.Events;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class VehicleCore :
	MonoBehaviourPun, IPunInstantiateMagicCallback,
	IBlockCoreRegistry, IBlockLifecycleListener, IControlCoreRegistry
{
	public static readonly List<VehicleCore> ActiveVehicles = new List<VehicleCore>();

	public UnityEvent OnVehicleDeath;

	private Rigidbody2D _body;

	private Dictionary<Vector2Int, GameObject> _posToBlock;
	private Dictionary<Vector2Int, GameObject> _rootPosToBlock;

	private List<ControlCore> _controlCores;

	private bool _loaded;
	private UnityEvent _loadEvent;
	private VehicleBlueprint _blueprint;
	private VehicleBlockConnectivityGraph _connectivityGraph;
	public bool Dead { get; private set; }

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();

		_posToBlock = new Dictionary<Vector2Int, GameObject>();
		_rootPosToBlock = new Dictionary<Vector2Int, GameObject>();
		_controlCores = new List<ControlCore>();
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_blueprint = JsonUtility.FromJson<VehicleBlueprint>((string) instantiationData[0]);
	}

	private void OnEnable()
	{
		ActiveVehicles.Add(this);
	}

	private void Start()
	{
		if (_blueprint != null)
		{
			LoadVehicle(_blueprint);
		}
	}

	private void OnDisable()
	{
		// Clean up when leaving room
		if (!Dead)
		{
			bool success = ActiveVehicles.Remove(this);
			if (!success)
			{
				Debug.LogError($"Failed to remove vehicle {gameObject} from active vehicle list");
			}
		}
	}

	public void LoadVehicle(VehicleBlueprint blueprint)
	{
		var blocks = new List<Tuple<VehicleBlueprint.BlockInstance, GameObject>>();
		float totalMass = 0f;
		Vector2 centerOfMass = Vector2.zero;
		var momentOfInertiaData = new LinkedList<Tuple<Vector2, float, float>>();

		// Instantiate blocks
		foreach (VehicleBlueprint.BlockInstance blockInstance in blueprint.Blocks)
		{
			if (!BlockDatabase.Instance.HasBlock(blockInstance.BlockId))
			{
				Debug.LogError($"Failed to load block by ID: {blockInstance.BlockId}");
				continue;
			}

			BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(blockInstance.BlockId).Spec;

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
				_posToBlock.Add(
					blockInstance.Position + TransformUtils.RotatePoint(localPosition, blockInstance.Rotation),
					blockObject
				);
			}

			blocks.Add(new Tuple<VehicleBlueprint.BlockInstance, GameObject>(blockInstance, blockObject));
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

		_body.mass = totalMass;
		_body.centerOfMass = centerOfMass;
		_body.inertia = momentOfInertia;

		transform.position -= (Vector3) centerOfMass;

		// Load config
		foreach (Tuple<VehicleBlueprint.BlockInstance, GameObject> tuple in blocks)
		{
			BlockConfigHelper.LoadConfig(tuple.Item1, tuple.Item2);
		}

		// Set up connectivity graph
		_connectivityGraph = new VehicleBlockConnectivityGraph(_blueprint.Blocks);

		_loaded = true;

		if (_loadEvent != null)
		{
			_loadEvent.Invoke();
			_loadEvent.RemoveAllListeners();
		}
	}

	public void AfterLoad(UnityAction action)
	{
		if (_loaded)
		{
			action();
		}
		else
		{
			_loadEvent ??= new UnityEvent();
			_loadEvent.AddListener(action);
		}
	}

	#region Block Management

	public void RegisterBlock(BlockCore blockCore)
	{
		if (!photonView.IsMine) return;
		// When the vehicle is loading, ignore everything as the calculation will be done by the loading routine.
		if (!_loaded) return;

		BlockCore core = blockCore.GetComponent<BlockCore>();
		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(core.BlockId).Spec;
		Vector2 blockCenter = blockCore.CenterOfMassPosition;
		AddMass(blockCenter, spec.Physics.Mass, spec.Physics.MomentOfInertia);
	}

	public void UnregisterBlock(BlockCore blockCore)
	{
		if (!photonView.IsMine) return;
		if (!_loaded) return;

		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(blockCore.BlockId).Spec;
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
	}

	private void AddMass(Vector2 position, float mass, float moment)
	{
		float totalMass = _body.mass + mass;
		Vector2 centerOfMass = totalMass > Mathf.Epsilon
			? (_body.centerOfMass * _body.mass + position * mass) / totalMass
			: Vector2.zero;
		float momentOfInertia = _body.inertia
		                        + _body.mass * (_body.centerOfMass - centerOfMass).sqrMagnitude
		                        + moment
		                        + mass * (position - centerOfMass).sqrMagnitude;

		_body.mass = totalMass;
		_body.centerOfMass = centerOfMass;
		_body.inertia = momentOfInertia;
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

	#endregion

	public void RegisterBlock(ControlCore block)
	{
		_controlCores.Add(block);
	}

	public void UnregisterBlock(ControlCore block)
	{
		bool success = _controlCores.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove control core block {block}");
		}

		if (_controlCores.Count <= 0)
		{
			Debug.Log($"{gameObject}: All control cores destroyed. Disabling controls.");
			Die();
		}
	}

	public void Die()
	{
		bool success = ActiveVehicles.Remove(this);
		if (!success)
		{
			Debug.LogError($"Failed to remove vehicle {this} from active vehicle list");
		}

		Dead = true;
		OnVehicleDeath.Invoke();
	}

	public IEnumerable<GameObject> GetAllBlocks() => _rootPosToBlock.Values;

	public GameObject GetBlockAt(Vector2Int localPosition) =>
		_posToBlock.TryGetValue(localPosition, out GameObject block) ? block : null;
}
}