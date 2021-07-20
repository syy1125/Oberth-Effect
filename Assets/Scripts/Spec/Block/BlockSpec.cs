using System;
using System.Reflection;
using Syy1125.OberthEffect.Spec.Block.Physics;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.Block
{
public struct BlockSpec
{
	public string BlockId;
	public InfoSpec Info;
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

		return _fallback.Create(type);
	}
}
}