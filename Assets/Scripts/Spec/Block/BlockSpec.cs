﻿using System;
using System.IO;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using Syy1125.OberthEffect.Spec.Block.Resource;
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
	public ResourceSpec Resource;
	public PropulsionSpec Propulsion;
	public TurretedWeaponSpec TurretedWeapon;
	public FixedWeaponSpec FixedWeapon;

	public VolatileSpec Volatile;

	public void GetBytes(Stream stream, ChecksumLevel level)
	{
		if (!Enabled && level < ChecksumLevel.Everything)
		{
			return;
		}
		else
		{
			ChecksumHelper.GetBytesFromFields(stream, this, level);
		}
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
		else if (type == typeof(ProjectileWeaponEffectSpec))
		{
			return new ProjectileWeaponEffectSpec
			{
				ClusterBaseAngles = new[] { 0f }
			};
		}


		return _fallback.Create(type);
	}
}
}