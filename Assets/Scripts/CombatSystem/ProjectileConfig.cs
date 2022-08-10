using System;
using System.Collections.Generic;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Foundation.Enums;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.CombatSystem
{
[Serializable]
public class ProjectileConfig
{
	public Vector2 ColliderSize;
	public float Damage;
	public DamagePattern DamagePattern;
	public string DamageTypeId;
	public float ArmorPierce; // Note that explosive damage will always have armor pierce of 1
	public float ExplosionRadius; // Only relevant for explosive damage
	public float Lifetime;

	public ColorScheme ColorScheme;
	public RendererSpec[] Renderers;
	public ParticleSystemSpec[] TrailParticles;
}

[Serializable]
public class NetworkedProjectileConfig : ProjectileConfig
{
	public PointDefenseTargetSpec PointDefenseTarget;
	public float HealthDamageScaling;

	#region Projectile Components

	internal static readonly Dictionary<string, (Type SpecType, Type ComponentType)> ComponentTypes = new();

	static NetworkedProjectileConfig()
	{
		ModLoader.OnAugmentSerializer += AugmentSerializer;
		ModLoader.OnResetMods += ResetComponentTypes;
	}

	private static void ResetComponentTypes()
	{
		ComponentTypes.Clear();
	}

	public static void Register<TSpec, TComponent>(string name) where TComponent : INetworkedProjectileComponent<TSpec>
	{
		Register(name, typeof(TSpec), typeof(TComponent));
	}

	public static void Register(string name, Type specType, Type componentType)
	{
		if (!typeof(INetworkedProjectileComponent<>).MakeGenericType(specType).IsAssignableFrom(componentType))
		{
			Debug.LogError(
				$"Mapped projectile component type `{componentType.FullName}` for key `{name}` does not implement `INetworkedProjectileComponent<{specType.FullName}>`"
			);
		}

		ComponentTypes.Add(name, (specType, componentType));
	}

	private static void AugmentSerializer(SerializerBuilder serializerBuilder, DeserializerBuilder deserializerBuilder)
	{
		foreach (var entry in ComponentTypes)
		{
			serializerBuilder.WithTagMapping($"!{entry.Key}", entry.Value.SpecType);
			deserializerBuilder.WithTagMapping($"!{entry.Key}", entry.Value.SpecType);
		}
	}

	public static Type GetSpecType(string name)
	{
		return ComponentTypes.TryGetValue(name, out var types) ? types.SpecType : null;
	}

	public static Type GetComponentType(string name)
	{
		return ComponentTypes.TryGetValue(name, out var types) ? types.ComponentType : null;
	}

	#endregion

	public Dictionary<string, object> ProjectileComponents = new();
}


public interface INetworkedProjectileComponent<in TSpec>
{
	void LoadSpec(TSpec spec);
}
}