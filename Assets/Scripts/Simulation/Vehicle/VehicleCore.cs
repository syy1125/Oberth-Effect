using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class VehicleCore : MonoBehaviourPun, IPunInstantiateMagicCallback, IBlockCoreRegistry, IBlockLifecycleListener
{
	private Rigidbody2D _body;

	private Dictionary<Vector2Int, GameObject> _posToBlock;

	private bool _loaded;
	private UnityEvent _loadEvent;
	private VehicleBlueprint _blueprint;

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();

		_posToBlock = new Dictionary<Vector2Int, GameObject>();
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_blueprint = JsonUtility.FromJson<VehicleBlueprint>((string) instantiationData[0]);
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
		foreach (VehicleBlueprint.BlockInstance block in blueprint.Blocks)
		{
			GameObject blockPrefab = BlockDatabase.Instance.GetBlock(block.BlockID);

			if (blockPrefab == null)
			{
				Debug.LogError($"Failed to load block by ID: {block.BlockID}");
				continue;
			}

			var rootLocation = new Vector2(block.X, block.Y);
			var rootLocationInt = new Vector2Int(block.X, block.Y);

			GameObject blockObject = Instantiate(blockPrefab, transform);
			blockObject.transform.localPosition = rootLocation;
			blockObject.transform.localRotation = RotationUtils.GetPhysicalRotation(block.Rotation);

			BlockInfo info = blockPrefab.GetComponent<BlockInfo>();
			Vector2 blockCenter = rootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);
			totalMass += info.Mass;
			centerOfMass += info.Mass * blockCenter;
			momentOfInertiaData.AddLast(new Tuple<Vector2, float, float>(blockCenter, info.Mass, info.MomentOfInertia));

			var blockCore = blockObject.GetComponent<BlockCore>();
			blockCore.OwnerId = photonView.OwnerActorNr;
			blockCore.RootLocation = rootLocationInt;
			blockCore.Rotation = block.Rotation;

			foreach (Vector3Int localPosition in info.Bounds.allPositionsWithin)
			{
				_posToBlock.Add(
					rootLocationInt + RotationUtils.RotatePoint(localPosition, block.Rotation), blockObject
				);
			}

			blocks.Add(new Tuple<VehicleBlueprint.BlockInstance, GameObject>(block, blockObject));
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
			JObject config = null;

			try
			{
				config = JObject.Parse(tuple.Item1.Config);
			}
			catch (JsonReaderException)
			{}

			foreach (MonoBehaviour behaviour in tuple.Item2.GetComponents<MonoBehaviour>())
			{
				if (behaviour is IConfigComponent component)
				{
					component.InitDefaultConfig();
					string configKey = ConfigUtils.GetConfigKey(component.GetType());

					if (config != null && config.ContainsKey(configKey))
					{
						component.ImportConfig((JObject) config[configKey]);
					}
				}
			}
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

		BlockInfo info = blockCore.GetComponent<BlockInfo>();
		Vector2 blockCenter = blockCore.CenterOfMassPosition;
		AddMass(blockCenter, info.Mass, info.MomentOfInertia);
	}

	public void UnregisterBlock(BlockCore blockCore)
	{
		if (!_loaded) return;

		BlockInfo info = blockCore.GetComponent<BlockInfo>();
		Vector2 blockCenter = blockCore.CenterOfMassPosition;
		AddMass(blockCenter, -info.Mass, -info.MomentOfInertia);
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
		photonView.RPC("DisableBlock", RpcTarget.AllBuffered, blockCore.RootLocation.x, blockCore.RootLocation.y);
	}

	[PunRPC]
	private void DisableBlock(int x, int y)
	{
		_posToBlock[new Vector2Int(x, y)].SetActive(false);
	}

	#endregion

	public IEnumerable<GameObject> GetAllBlocks() => _posToBlock.Values;

	public GameObject GetBlockAt(Vector2Int localPosition) =>
		_posToBlock.TryGetValue(localPosition, out GameObject block) ? block : null;
}
}