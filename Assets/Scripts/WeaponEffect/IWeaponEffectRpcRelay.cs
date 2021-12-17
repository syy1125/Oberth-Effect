using Photon.Pun;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IWeaponEffectRpcRelay : IEventSystemHandler
{
	void InvokeWeaponEffectRpc(string methodName, RpcTarget rpcTarget, params object[] parameters);
}
}