using UnityEngine;

namespace Syy1125.OberthEffect.Spec.Database
{
// Mainly for testing purposes, this script ensures that all mod data is loaded before databases start initializing
public class DatabaseInitializer : MonoBehaviour
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