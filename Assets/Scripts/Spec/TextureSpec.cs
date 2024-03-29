﻿using System;
using System.IO;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec
{
[CreateSchemaFile("TextureSpecSchema")]
[ContainsPath]
public struct TextureSpec : ICustomChecksum
{
	[IdField]
	public string TextureId;
	[ValidateFilePath]
	[ResolveAbsolutePath]
	public string ImagePath;
	public Vector2 Pivot;
	public float PixelsPerUnit;
	public bool ApplyVehicleColors;

	public void GetBytes(Stream stream, ChecksumLevel level)
	{
		if (level < ChecksumLevel.Strict) return;

		ChecksumHelper.GetBytesFromString(stream, TextureId);

		Stream image = File.OpenRead(ImagePath);
		image.CopyTo(stream);

		ChecksumHelper.GetBytesFromVector(stream, Pivot);
		ChecksumHelper.GetBytesFromPrimitive(stream, PixelsPerUnit);
		ChecksumHelper.GetBytesFromPrimitive(stream, ApplyVehicleColors);
	}
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