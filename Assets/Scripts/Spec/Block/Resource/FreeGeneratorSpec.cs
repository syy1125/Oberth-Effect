﻿using System.Collections.Generic;
using Syy1125.OberthEffect.Spec.ControlGroup;
using Syy1125.OberthEffect.Spec.Unity;

namespace Syy1125.OberthEffect.Spec.Block.Resource
{
public class FreeGeneratorSpec
{
	public Dictionary<string, float> GenerationRate;
	public ControlConditionSpec ActivationCondition;
	public RendererSpec[] ActivationRenderers;
}
}