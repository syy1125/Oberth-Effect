﻿using System;

namespace Syy1125.OberthEffect.Spec.ModLoading
{
[Serializable]
public struct ModSpec
{
	public string DisplayName;
	public string Version;
	public string Description;
	public bool AllowDuplicateDefs;
}
}