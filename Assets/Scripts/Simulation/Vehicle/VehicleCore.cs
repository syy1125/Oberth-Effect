using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

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

	private List<ControlCore> _controlCores;

	private bool _loaded;
	private UnityEvent _loadEvent;
	private VehicleBlueprint _blueprint;

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();

		_posToBlock = new Dictionary<Vector2Int, GameObject>();
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
		// When the vehicle is loading, ignore everything as the calculation will be done by the loading routine.
		if (!_loaded) return;

		BlockCore core = blockCore.GetComponent<BlockCore>();
		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(core.BlockId).Spec;
		Vector2 blockCenter = blockCore.CenterOfMassPosition;
		AddMass(blockCenter, spec.Physics.Mass, spec.Physics.MomentOfInertia);
	}

	public void UnregisterBlock(BlockCore blockCore)
	{
		if (!_loaded) return;

		BlockCore core = blockCore.GetComponent<BlockCore>();
		BlockSpec spec = BlockDatabase.Instance.GetSpecInstance(core.BlockId).Spec;
		Vector2 blockCenter = blockCore.CenterOfMassPosition;
		// Remove the block by adding negative mass
		// TODO: Work out the physics to verify that this actually works
		AddMass(blockCenter, -spec.Physics.Mass, -spec.Physics.MomentOfInertia);
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
		photonView.RPC("DisableBlock", RpcTarget.AllBuffered, blockCore.RootPosition.x, blockCore.RootPosition.y);
	}

	[PunRPC]
	private void DisableBlock(int x, int y)
	{
		_posToBlock[new Vector2Int(x, y)].SetActive(false);
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

			success = ActiveVehicles.Remove(this);
			if (!success)
			{
				Debug.LogError($"Failed to remove vehicle {this} from active vehicle list");
			}

			OnVehicleDeath.Invoke();
		}
	}

	public IEnumerable<GameObject> GetAllBlocks() => _posToBlock.Values;

	public GameObject GetBlockAt(Vector2Int localPosition) =>
		_posToBlock.TryGetValue(localPosition, out GameObject block) ? block : null;
}
}