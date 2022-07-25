// using System;
// using System.Collections;
// using Photon.Pun;
// using Syy1125.OberthEffect.CombatSystem;
// using Syy1125.OberthEffect.Foundation;
// using Syy1125.OberthEffect.Foundation.Enums;
// using Syy1125.OberthEffect.Foundation.Physics;
// using Syy1125.OberthEffect.Lib.Pid;
// using Syy1125.OberthEffect.Lib.Utils;
// using Syy1125.OberthEffect.Spec.Block.Weapon;
// using Syy1125.OberthEffect.Spec.Unity;
// using UnityEngine;
//
// namespace Syy1125.OberthEffect.WeaponEffect
// {
// public struct MissileConfig
// {
// 	public Vector2 ColliderSize;
// 	public float Damage;
// 	public DamageType DamageType;
// 	public float ArmorPierce;
// 	public float ExplosionRadius;
// 	public float ProximityFuseRadius;
// 	public float Lifetime;
//
// 	public bool HasTarget;
// 	public int TargetPhotonId;
//
// 	public float MaxAcceleration;
// 	public float MaxAngularAcceleration;
// 	public float ThrustActivationDelay;
// 	public MissileGuidanceAlgorithm GuidanceAlgorithm;
// 	public float GuidanceActivationDelay;
// 	public MissileRetargetingBehaviour RetargetingBehaviour;
//
// 	public bool IsPointDefenseTarget;
// 	public float MaxHealth;
// 	public float ArmorValue;
// 	public float HealthDamageScaling;
//
// 	public RendererSpec[] Renderers;
// 	public ParticleSystemSpec[] PropulsionParticles;
// }
//
// [RequireComponent(typeof(PhotonView))]
// [RequireComponent(typeof(Rigidbody2D))]
// [RequireComponent(typeof(BoxCollider2D))]
// [RequireComponent(typeof(DamagingProjectile))]
// public class Missile : MonoBehaviourPun, IPunInstantiateMagicCallback
// {
//
// 	private IEnumerator LateFixedUpdate()
// 	{
// 		while (isActiveAndEnabled)
// 		{
// 			yield return new WaitForFixedUpdate();
//
// 			if (Time.time - _initTime < _config.GuidanceActivationDelay) continue;
//
// 			if (photonView.IsMine)
// 			{
// 				switch (_config.RetargetingBehaviour)
// 				{
// 					case MissileRetargetingBehaviour.Never:
// 						break;
// 					case MissileRetargetingBehaviour.IfInvalid:
// 						if (!HasValidTarget())
// 						{
// 							RetargetMissile();
// 						}
//
// 						break;
// 					case MissileRetargetingBehaviour.Always:
// 						RetargetMissile();
// 						break;
// 					default:
// 						throw new ArgumentOutOfRangeException();
// 				}
//
// 				if (HasValidTarget())
// 				{
// 					if (
// 						_config.DamageType == DamageType.Explosive
// 						&& _config.ProximityFuseRadius > Mathf.Epsilon
// 					)
// 					{
// 						Vector2 relativePosition = _target.GetEffectivePosition() - _ownBody.worldCenterOfMass;
// 						Vector2 relativeVelocity = _target.GetEffectiveVelocity() - _ownBody.velocity;
// 						Vector2 nextPosition = relativePosition + relativeVelocity * Time.fixedDeltaTime;
//
// 						if (
// 							relativePosition.magnitude < _config.ProximityFuseRadius
// 							&& nextPosition.sqrMagnitude > relativePosition.sqrMagnitude
// 						)
// 						{
// 							GetComponent<DamagingProjectile>().LifetimeDespawn();
// 							yield break;
// 						}
// 					}
// 				}
// 			}
// 		}
// 	}
//
// 	private void RetargetMissile()
// 	{
// 		if (Launcher == null || Launcher.Equals(null) || !Launcher.enabled)
// 		{
// 			photonView.RPC(nameof(SetTargetId), RpcTarget.All, (int?) null);
// 			return;
// 		}
//
// 		if (Launcher.TargetPhotonId == null)
// 		{
// 			if (HasValidTarget())
// 			{
// 				photonView.RPC(nameof(SetTargetId), RpcTarget.All, Launcher.TargetPhotonId);
// 			}
// 		}
// 		else
// 		{
// 			if (!HasValidTarget() || Launcher.TargetPhotonId.Value != _target.photonView.ViewID)
// 			{
// 				photonView.RPC(nameof(SetTargetId), RpcTarget.All, Launcher.TargetPhotonId);
// 			}
// 		}
// 	}
// }
// }