using System;
using Syy1125.OberthEffect.Spec.Block.Physics;
using Syy1125.OberthEffect.Spec.Block.Propulsion;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.Block
{
public class BlockSpec
{
	public string BlockId;
	public bool Enabled;
	public InfoSpec Info;
	public RendererSpec[] Renderers;
	public ConstructionSpec Construction;
	public PhysicsSpec Physics;
	public CombatSpec Combat;
	public PropulsionSpec Propulsion;
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
		if (type == typeof(InfoSpec))
		{
			return new InfoSpec
			{
				PreviewScale = 1
			};
		}
		else if (type == typeof(RendererSpec))
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