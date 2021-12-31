using System;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
[Serializable]
public struct RendererSpec
{
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateTextureId]
	public string TextureId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public Vector2 Offset;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public float Rotation;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public Vector2 Scale;
}
}