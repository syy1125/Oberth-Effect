using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IBlockRpcRelay : IEventSystemHandler
{
	PhotonView photonView { get; }

	void InvokeBlockRpc(
		Vector2Int position, Type componentType, string methodName,
		Player target, params object[] parameters
	);

	void InvokeBlockRpc(
		Vector2Int position, Type componentType, string methodName,
		RpcTarget target, params object[] parameters
	);
}
}