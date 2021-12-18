using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Common.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Database;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks.Weapons
{
public class FixedWeapon : AbstractWeapon, IHasDebrisState, ITooltipProvider
{
	public const string CLASS_KEY = "FixedWeapon";

	public void LoadSpec(FixedWeaponSpec spec)
	{
		if (spec.ProjectileWeaponEffect != null)
		{
			LoadProjectileWeapon(spec.ProjectileWeaponEffect);
		}
		else if (spec.BurstBeamWeaponEffect != null)
		{
			LoadBurstBeamWeapon(spec.BurstBeamWeaponEffect);
		}
	}

	protected override void SetWeaponEffectTransform(GameObject weaponEffectObject, AbstractWeaponEffectSpec spec)
	{
		var weaponEffectTransform = weaponEffectObject.transform;
		weaponEffectTransform.SetParent(transform);
		weaponEffectTransform.localPosition = spec.FiringPortOffset;
		weaponEffectTransform.localRotation = Quaternion.identity;
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (enabled)
		{
			WeaponEmitter.EmitterFixedUpdate(Core.IsMine, Firing);
			yield return new WaitForFixedUpdate();
		}
	}

	public JObject SaveDebrisState()
	{
		return null;
	}

	public void LoadDebrisState(JObject state)
	{
		enabled = false;
	}

	public string GetTooltip()
	{
		StringBuilder builder = new StringBuilder();

		builder.AppendLine("Fixed Weapon");

		builder.Append(WeaponEmitter.GetEmitterTooltip());

		AppendAggregateDamageInfo(builder);

		return builder.ToString();
	}
}
}