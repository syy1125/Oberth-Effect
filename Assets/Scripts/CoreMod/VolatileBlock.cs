﻿using System.Text;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation.ControlCondition;
using Syy1125.OberthEffect.Foundation.Utils;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod
{
[CreateSchemaFile("VolatileSpecSchema")]
public class VolatileSpec
{
	// Information only - up to the exact components of the block to control explosion behaviour when the block is actually destroyed.
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public bool AlwaysExplode;
	public ControlConditionSpec ActivationCondition;
	public Vector2 ExplosionOffset;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxRadius;
	[ValidateRangeFloat(0f, float.PositiveInfinity)]
	public float MaxDamage;
	[ValidateDamageTypeId]
	[SchemaDescription("The damage type of the explosion when the block is destroyed")]
	public string DamageTypeId;
}

public class VolatileBlock : MonoBehaviour, IBlockComponent<VolatileSpec>, IBlockDestructionEffect, ITooltipComponent
{
	private bool _alwaysExplode;
	private IControlCondition _activationCondition;
	private Vector2 _explosionOffset;
	private float _maxRadius;
	private float _maxDamage;
	private string _damageTypeId;

	public void LoadSpec(VolatileSpec spec, in BlockContext context)
	{
		_alwaysExplode = spec.AlwaysExplode;
		_activationCondition = ControlConditionHelper.CreateControlCondition(spec.ActivationCondition);
		_explosionOffset = spec.ExplosionOffset;
		_maxRadius = spec.MaxRadius;
		_maxDamage = spec.MaxDamage;

		if (context.Environment == BlockEnvironment.Preview)
		{
			var explosionPreview = new GameObject("ExplosionPreview");
			explosionPreview.transform.SetParent(transform);
			explosionPreview.transform.localPosition = _explosionOffset;
			explosionPreview.transform.localRotation = Quaternion.identity;
			explosionPreview.transform.localScale = new(_maxRadius * 2, _maxRadius * 2, 1);

			var explosionSprite = explosionPreview.AddComponent<SpriteRenderer>();
			explosionSprite.sprite = TextureDatabase.Instance.ExplosionSprite;
			Color explosionColor = Color.red;
			explosionColor.a = 0.2f;
			explosionSprite.color = explosionColor;
		}

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
			radius, damage, _damageTypeId, -1
		);
	}

	public bool GetTooltip(StringBuilder builder, string indent)
	{
		if (_alwaysExplode)
		{
			builder.AppendLine(
				$"{indent}<color=red>Volatile</color>: Explodes for {_maxDamage:F0} damage in a {PhysicsUnitUtils.FormatLength(_maxRadius)} radius when destroyed."
			);
		}
		else
		{
			builder.AppendLine(
				$"{indent}<color=orange>Sometimes volatile</color>: Can explode for up to {_maxDamage:F0} damage in a {PhysicsUnitUtils.FormatLength(_maxRadius)} radius when destroyed."
			);
		}

		return true;
	}
}
}