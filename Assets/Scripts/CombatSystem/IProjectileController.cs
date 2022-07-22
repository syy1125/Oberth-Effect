using System;
using Photon.Pun;
using Photon.Realtime;

namespace Syy1125.OberthEffect.CombatSystem
{
public interface IProjectileController
{
	bool IsMine { get; }
	int OwnerId { get; }
	void InvokeProjectileRpc(Type componentType, string methodName, Player target, params object[] parameters);
	void InvokeProjectileRpc(Type componentType, string methodName, RpcTarget target, params object[] parameters);
	void RequestDestroyProjectile();
}
}