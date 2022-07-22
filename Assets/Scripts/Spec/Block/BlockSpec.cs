using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.Spec.Block.Weapon;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.Unity;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.Block
{
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

	internal static readonly Dictionary<string, (Type SpecType, Type ComponentType)> ComponentTypes = new();

	public static void Register<TSpec, TComponent>(string name) where TComponent : IBlockComponent<TSpec>
	{
		Register(name, typeof(TSpec), typeof(TComponent));
	}

	public static void Register(string name, Type specType, Type componentType)
	{
		if (!typeof(IBlockComponent<>).MakeGenericType(specType).IsAssignableFrom(componentType))
		{
			Debug.LogError(
				$"Mapped component type `{componentType.FullName}` for component key `{name}` does not implement `IBlockComponent<{specType.FullName}>` and may not load correctly!"
			);
		}

		ComponentTypes.Add(name, (specType, componentType));
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

	public Dictionary<string, object> BlockComponents = new();

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