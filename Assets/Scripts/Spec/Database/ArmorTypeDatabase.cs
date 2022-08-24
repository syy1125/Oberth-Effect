using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class ArmorTypeDatabase : MonoBehaviour, IGameContentDatabase
{
	public static ArmorTypeDatabase Instance { get; private set; }
	private Dictionary<string, SpecInstance<ArmorTypeSpec>> _specs;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}
	}

	public void Reload()
	{
		_specs = ModLoader.ArmorTypePipeline.Results
			.ToDictionary(instance => instance.Spec.ArmorTypeId, instance => instance);
		Debug.Log($"Loaded {_specs.Count} armor type specs");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public bool ContainsId(string id)
	{
		return id != null && _specs.ContainsKey(id);
	}

	public ArmorTypeSpec GetSpec(string armorTypeId)
	{
		return armorTypeId != null && _specs.TryGetValue(armorTypeId, out var instance)
			? instance.Spec
			: new()
			{
				ArmorTypeId = armorTypeId,
				DisplayName = "ERROR UNKNOWN ARMOR TYPE",
				ArmorValue = 1f
			};
	}

	public float GetDamageModifier(string damageType, float armorPierce, string armorType)
	{
		ArmorTypeSpec spec = GetSpec(armorType);
		float damageModifier = Mathf.Min(armorPierce / spec.ArmorValue, 1f);

		if (spec.DamageModifiers != null && spec.DamageModifiers.TryGetValue(damageType, out float armorTypeModifier))
		{
			damageModifier *= armorTypeModifier;
		}

		return damageModifier;
	}

	public string GetDamageModifierTooltip(string armorTypeId)
	{
		if (!ContainsId(armorTypeId)) return null;
		var armorTypeSpec = _specs[armorTypeId].Spec;
		if (armorTypeSpec.DamageModifiers is not { Count: > 0 }) return null;

		return string.Join(
			", ",
			armorTypeSpec.DamageModifiers
				.Where(entry => DamageTypeDatabase.Instance.ContainsId(entry.Key))
				.Select(
					entry =>
					{
						var damageTypeSpec = DamageTypeDatabase.Instance.GetSpec(entry.Key);
						return
							$"{entry.Value:0.#%} damage from {damageTypeSpec.WrapColorTag(damageTypeSpec.DisplayName)} sources";
					}
				)
		);
	}
}
}