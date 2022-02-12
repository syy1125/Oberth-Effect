using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.ControlCondition;
using Syy1125.OberthEffect.Common.Physics;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class VolatileBlock : MonoBehaviour, IBlockDestructionEffect, ITooltipProvider
{
	private bool _alwaysExplode;
	private IControlCondition _activationCondition;
	private Vector2 _explosionOffset;
	private float _maxRadius;
	private float _maxDamage;

	public void LoadSpec(VolatileSpec spec)
	{
		_alwaysExplode = spec.AlwaysExplode;
		_activationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);
		_explosionOffset = spec.ExplosionOffset;
		_maxRadius = spec.MaxRadius;
		_maxDamage = spec.MaxDamage;

		GetComponentInParent<IControlConditionProvider>()?
			.MarkControlGroupsActive(_activationCondition.GetControlGroups());
	}

	public void OnDestroyedByDamage()
	{
		var provider = GetComponentInParent<IControlConditionProvider>();
		if (provider == null || !provider.IsConditionTrue(_activationCondition))
		{
			return;
		}

		float radius = _maxRadius;
		float damage = _maxDamage;

		if (Mathf.Approximately(radius, 0f) || Mathf.Approximately(damage, 0f)) return;

		var photonView = GetComponentInParent<PhotonView>();

		Debug.Log($"Block \"{gameObject}\" is exploding for {damage} damage in {radius} game unit radius.");
		ExplosionManager.Instance.CreateExplosionAt(
			photonView.ViewID, photonView.transform.InverseTransformPoint(transform.TransformPoint(_explosionOffset)),
			radius, damage, -1
		);
	}

	public string GetTooltip()
	{
		return _alwaysExplode
			? $"<color=\"red\">Volatile</color>: Explodes for {_maxDamage:F0} damage in a {PhysicsUnitUtils.FormatLength(_maxRadius)} radius when destroyed."
			: $"<color=\"orange\">Sometimes volatile</color>: Can explode for up to {_maxDamage:F0} damage in a {PhysicsUnitUtils.FormatLength(_maxRadius)} radius when destroyed.";
	}
}
}