﻿using Syy1125.OberthEffect.Spec.SchemaGen.Attributes;
using Syy1125.OberthEffect.Spec.Validation.Attributes;

namespace Syy1125.OberthEffect.Spec.Block
{
[CreateSchemaFile("SoundCurveSpecSchema")]
public class SoundCurveSpec
{
	[ValidateNonNull]
	[ValidateSoundId]
	public string SoundId;
	[ValidateRangeFloat(0f, 1f)]
	public float MinVolume = 0f;
	[ValidateRangeFloat(0f, 1f)]
	public float MaxVolume = 1f;
}
}