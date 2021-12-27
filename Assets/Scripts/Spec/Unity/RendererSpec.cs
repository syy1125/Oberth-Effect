using System;
using Syy1125.OberthEffect.Spec.Checksum;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
[Serializable]
public struct RendererSpec
{
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public string TextureId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public Vector2 Offset;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public float Rotation;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public Vector2 Scale;
}
}