using System;
using System.ComponentModel;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec
{
public struct TextureSpec
{
	public string TextureId;
	public string ImagePath;
	public Vector2 Pivot;
	public float PixelsPerUnit;
	public bool ApplyVehicleColors;
}

public class TextureSpecFactory : IObjectFactory
{
	private readonly IObjectFactory _fallback;

	public TextureSpecFactory(IObjectFactory fallback)
	{
		_fallback = fallback;
	}

	public object Create(Type type)
	{
		if (type == typeof(TextureSpec))
		{
			return new TextureSpec
			{
				Pivot = new Vector2(0.5f, 0.5f)
			};
		}
		else
		{
			return _fallback.Create(type);
		}
	}
}
}