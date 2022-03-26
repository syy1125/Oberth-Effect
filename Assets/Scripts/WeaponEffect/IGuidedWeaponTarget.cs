using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.WeaponEffect
{
public interface IGuidedWeaponTarget
{
	public PhotonView photonView { get; }

	Vector2 GetEffectivePosition();
	Vector2 GetEffectiveVelocity();

	T[] GetComponents<T>();
}
}