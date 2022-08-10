using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
public class DamageTypeDatabase : MonoBehaviour, IGameContentDatabase
{
	public static DamageTypeDatabase Instance { get; private set; }

	private Dictionary<string, SpecInstance<DamageTypeSpec>> _specs;

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
		_specs = ModLoader.DamageTypePipeline.Results
			.ToDictionary(instance => instance.Spec.DamageTypeId, instance => instance);
		Debug.Log($"Loaded {_specs.Count} damage types");
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

	public DamageTypeSpec GetSpec(string damageTypeId)
	{
		return damageTypeId != null && _specs.TryGetValue(damageTypeId, out var instance)
			? instance.Spec
			: new()
			{
				DamageTypeId = damageTypeId,
				DisplayName = "ERROR UNKNOWN DAMAGE TYPE",
				DisplayColor = "red",
			};
	}
}
}