using Photon.Pun;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public interface IWeaponEffectRpcRelay
{
	void InvokeWeaponEffectRpc(string methodName, RpcTarget rpcTarget, params object[] parameters);
}
}