﻿using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class ModLoaderTest : MonoBehaviour
{
	private void Awake()
	{
		if (!ModLoader.DataReady)
		{
			ModLoader.Init();
			ModLoader.LoadModList();
			ModLoader.LoadAllEnabledContent();
		}
	}
}
}