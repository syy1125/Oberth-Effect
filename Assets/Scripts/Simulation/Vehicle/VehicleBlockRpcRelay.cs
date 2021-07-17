using System;
using System.Reflection;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(VehicleCore))]
public class VehicleBlockRpcRelay : MonoBehaviourPun, IBlockRpcRelay
{
	private VehicleCore _core;

	private void Awake()
	{
		_core = GetComponent<VehicleCore>();
	}

	public void InvokeBlockRpc(
		Vector2Int position, Type componentType, string methodName,
		RpcTarget target, params object[] parameters
	)
	{
		photonView.RPC(
			"BlockRpc", target,
			position.x, position.y, componentType.ToString(), methodName, parameters
		);
	}

	[PunRPC]
	public void BlockRpc(int x, int y, string type, string methodName, object[] parameters)
	{
		GameObject blockObject = _core.GetBlockAt(new Vector2Int(x, y));

		if (blockObject == null)
		{
			Debug.LogError($"No block exists at ({x}, {y}) in {gameObject.name}");
			return;
		}

		var component = blockObject.GetComponent(type);
		var method = component.GetType().GetMethod(
			methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
		);

		if (method == null)
		{
			Debug.LogError($"Method {methodName} does not exist for {component.GetType()}");
			return;
		}

		method.Invoke(component, parameters);
	}
}
}