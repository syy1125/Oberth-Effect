using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Lib.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(ConstructBlockManager))]
public class VehicleCore :
	MonoBehaviourPun, IPunInstantiateMagicCallback, IControlCoreRegistry
{
	public static readonly List<VehicleCore> ActiveVehicles = new List<VehicleCore>();

	public UnityEvent OnVehicleDeath;

	private List<ControlCore> _controlCores;

	private bool _loaded;
	private UnityEvent _loadEvent;
	private VehicleBlueprint _blueprint;
	public bool IsDead { get; private set; }

	private void Awake()
	{
		_controlCores = new List<ControlCore>();
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		_blueprint = JsonUtility.FromJson<VehicleBlueprint>(CompressionUtils.Decompress((byte[]) instantiationData[0]));
		name = $"{photonView.Owner.NickName} {_blueprint.Name}";
	}

	private void OnEnable()
	{
		ActiveVehicles.Add(this);
	}

	private void Start()
	{
		if (_blueprint != null)
		{
			GetComponent<ConstructBlockManager>().LoadBlocks(_blueprint.Blocks);
			transform.position -= transform.TransformVector(GetComponent<Rigidbody2D>().centerOfMass);

			_loaded = true;
			if (_loadEvent != null)
			{
				_loadEvent.Invoke();
				_loadEvent.RemoveAllListeners();
			}
		}
	}

	private void OnDisable()
	{
		// Clean up when leaving room
		if (!IsDead)
		{
			bool success = ActiveVehicles.Remove(this);
			if (!success)
			{
				Debug.LogError($"Failed to remove vehicle {gameObject} from active vehicle list");
			}
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

	#region Control Cores

	public void RegisterBlock(ControlCore block)
	{
		if (!photonView.IsMine) return;

		_controlCores.Add(block);
	}

	public void UnregisterBlock(ControlCore block)
	{
		if (!photonView.IsMine) return;

		bool success = _controlCores.Remove(block);
		if (!success)
		{
			Debug.LogError($"Failed to remove control core block {block}");
		}

		if (_controlCores.Count <= 0)
		{
			Debug.Log($"{gameObject}: All control cores destroyed. Disabling controls.");
			photonView.RPC(nameof(DisableVehicle), RpcTarget.AllBuffered);
		}
	}

	#endregion

	[PunRPC]
	public void DisableVehicle()
	{
		bool success = ActiveVehicles.Remove(this);
		if (!success)
		{
			Debug.LogError($"Failed to remove vehicle {this} from active vehicle list");
		}

		IsDead = true;
		foreach (var listener in GetComponents<IVehicleDeathListener>())
		{
			listener.OnVehicleDeath();
		}

		OnVehicleDeath.Invoke();
	}
}
}