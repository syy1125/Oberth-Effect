using System;
using System.Reflection;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Blocks;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(ConstructBlockManager))]
public class BlockRpcRelay : MonoBehaviourPun, IBlockRpcRelay
{
	public void InvokeBlockRpc(
		Vector2Int position, Type componentType, string methodName, Player target, params object[] parameters
	)
	{
		photonView.RPC(
			nameof(ReceiveBlockRpc), target,
			position.x, position.y, componentType.ToString(), methodName, parameters
		);
	}

	public void InvokeBlockRpc(
		Vector2Int position, Type componentType, string methodName,
		RpcTarget target, params object[] parameters
	)
	{
		photonView.RPC(
			nameof(ReceiveBlockRpc), target,
			position.x, position.y, componentType.ToString(), methodName, parameters
		);
	}

	[PunRPC]
	public void ReceiveBlockRpc(int x, int y, string componentType, string methodName, object[] parameters)
	{
		GameObject blockObject = GetComponent<ConstructBlockManager>().GetBlockOccupying(new Vector2Int(x, y));

		if (blockObject == null)
		{
			Debug.LogError($"No block exists at ({x}, {y}) in {gameObject.name}");
			return;
		}

		var component = blockObject.GetComponent(componentType);
		if (component == null)
		{
			Debug.LogError(
				$"Component of type `{componentType}` does not exist at {blockObject.name} ({x}, {y}) in {gameObject.name}"
			);
			return;
		}

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