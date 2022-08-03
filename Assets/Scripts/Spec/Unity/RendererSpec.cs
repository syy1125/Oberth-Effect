using System;
using Syy1125.OberthEffect.Spec.Checksum;
using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;
using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Unity
{
[Serializable]
[CreateSchemaFile("RendererSpecSchema")]
public struct RendererSpec
{
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[ValidateTextureId]
	public string TextureId;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public Vector2 Offset;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	[SchemaDescription("Rotation in degrees counterclockwise.")]
	public float Rotation;
	[RequireChecksumLevel(ChecksumLevel.Strict)]
	public Vector2 Scale;
}
}