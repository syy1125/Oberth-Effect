using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.Block
{
[CreateSchemaFile("BlockSpecSchema")]
[CustomSchemaGeneration]
public class BlockSpec : ICustomChecksum
{
	[IdField]
	public string BlockId;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public bool Enabled;
	[RequireChecksumLevel(ChecksumLevel.Everything)]
	[ValidateBlockCategoryId]
	public string CategoryId;
	[ValidateRangeInt(0, int.MaxValue)]
	public int Cost;

	[RequireChecksumLevel(ChecksumLevel.Everything)]
	public InfoSpec Info;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public RendererSpec[] Renderers;
	public ConstructionSpec Construction;
	public PhysicsSpec Physics;
	public CombatSpec Combat;
	public ControlCoreSpec ControlCore;

	#region Block Component Types

	// Maps serialization key to spec type and block component type.
	private static readonly Dictionary<string, (Type SpecType, Type ComponentType)> SpecComponentTypes = new();
	// Maps block component type string to the type itself.
	private static readonly Dictionary<string, Type> DynamicComponentTypes = new();

	static BlockSpec()
	{
		ModLoader.OnAugmentSerializer += AugmentSerializer;
		ModLoader.OnResetMods += ResetComponentTypes;
	}

	private static void ResetComponentTypes()
	{
		SpecComponentTypes.Clear();
		DynamicComponentTypes.Clear();
	}

	public static void Register<TSpec, TComponent>(string name) where TComponent : IBlockComponent<TSpec>
	{
		Register(name, typeof(TSpec), typeof(TComponent));
	}

	public static void Register(string name, Type specType, Type componentType)
	{
		if (!typeof(IBlockComponent<>).MakeGenericType(specType).IsAssignableFrom(componentType))
		{
			Debug.LogError(
				$"Mapped block component type `{componentType.FullName}` for key `{name}` does not implement `IBlockComponent<{specType.FullName}>` and may not load correctly!"
			);
		}

		SpecComponentTypes.Add(name, (specType, componentType));
		DynamicComponentTypes.Add(componentType.ToString(), componentType);
	}

	public static bool DeserializeComponentType(string typeName, out Type type)
	{
		return DynamicComponentTypes.TryGetValue(typeName, out type);
	}

	private static void AugmentSerializer(SerializerBuilder serializerBuilder, DeserializerBuilder deserializerBuilder)
	{
		foreach (var entry in SpecComponentTypes)
		{
			serializerBuilder.WithTagMapping($"!{entry.Key}", entry.Value.SpecType);
			deserializerBuilder.WithTagMapping($"!{entry.Key}", entry.Value.SpecType);
		}
	}

	public static Type GetSpecType(string name)
	{
		return SpecComponentTypes.TryGetValue(name, out var types) ? types.SpecType : null;
	}

	public static Type GetComponentType(string name)
	{
		return SpecComponentTypes.TryGetValue(name, out var types) ? types.ComponentType : null;
	}

	#endregion

	[HideInSchema]
	public Dictionary<string, object> BlockComponents = new();

	public static Dictionary<string, object> GenerateSchemaObject()
	{
		var schema = SchemaGenerator.GenerateSchemaObjectFromMembers(typeof(BlockSpec));

		Dictionary<string, object> componentSpecs = new();

		foreach ((string name, (Type specType, Type componentType)) in SpecComponentTypes)
		{
			componentSpecs.Add(name, SchemaGenerator.GenerateSchemaMember(specType));
		}

		var properties = (Dictionary<string, object>) schema["properties"];

		properties.Add(
			"BlockComponents", new Dictionary<string, object>
			{
				{ "type", "object" },
				{ "properties", componentSpecs }
			}
		);

		return schema;
	}

	public void GetBytes(Stream stream, ChecksumLevel level)
	{
		if (!Enabled && level < ChecksumLevel.Everything)
		{
			return;
		}

		ChecksumHelper.GetBytesFromFields(stream, this, level);
	}
}

public class BlockSpecFactory : IObjectFactory
{
	private readonly IObjectFactory _fallback;

	public BlockSpecFactory(IObjectFactory fallback)
	{
		_fallback = fallback;
	}

	public object Create(Type type)
	{
		if (type == typeof(RendererSpec))
		{
			return new RendererSpec
			{
				Scale = Vector2.one
			};
		}
		else if (type == typeof(ParticleSystemSpec))
		{
			return new ParticleSystemSpec
			{
				EmissionRateOverTime = 50
			};
		}


		return _fallback.Create(type);
	}
}
}