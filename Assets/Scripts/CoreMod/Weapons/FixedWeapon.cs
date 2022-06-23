using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using UnityEngine;

namespace Syy1125.OberthEffect.CoreMod.Weapons
{
public class FixedWeaponSpec : AbstractWeaponSpec
{}

public class FixedWeapon : AbstractWeapon,
	IBlockComponent<FixedWeaponSpec>,
	IHasDebrisState,
	ITooltipProvider
{
	public const string CLASS_KEY = "FixedWeapon";

	public void LoadSpec(FixedWeaponSpec spec, in BlockContext context)
	{
		if (spec.ProjectileWeaponEffect != null)
		{
			LoadProjectileWeapon(spec.ProjectileWeaponEffect, context);
		}
		else if (spec.BurstBeamWeaponEffect != null)
		{
			LoadBurstBeamWeapon(spec.BurstBeamWeaponEffect, context);
		}
		else if (spec.MissileLauncherEffect != null)
		{
			LoadMissileWeapon(spec.MissileLauncherEffect, context);
		}

		DefaultBinding = spec.DefaultBinding;
	}

	protected override void SetWeaponEffectTransform(GameObject weaponEffectObject, AbstractWeaponEffectSpec spec)
	{
		var weaponEffectTransform = weaponEffectObject.transform;
		weaponEffectTransform.SetParent(transform);
		weaponEffectTransform.localPosition = spec.FiringPortOffset;
		weaponEffectTransform.localRotation = Quaternion.identity;
		weaponEffectTransform.localScale = Vector3.one;
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return new WaitForFixedUpdate();

		while (isActiveAndEnabled)
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