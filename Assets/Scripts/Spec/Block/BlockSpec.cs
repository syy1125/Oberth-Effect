﻿using System;
using Syy1125.OberthEffect.Spec.Block.Physics;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct BlockSpec
{
	public string BlockId;
	public bool Enabled;
	public InfoSpec Info;
	public RendererSpec[] Renderers;
	public ConstructionSpec Construction;
	public PhysicsSpec Physics;
	public CombatSpec Combat;
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


		return _fallback.Create(type);
	}
}
}