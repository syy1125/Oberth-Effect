using Photon.Pun;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public interface IWeaponLauncherRpcRelay
{
	void InvokeWeaponLauncherRpc(string methodName, RpcTarget rpcTarget, params object[] parameters);
}
}