using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Database;
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

	private void Start()
	{
		Debug.Log(VehicleResourceDatabase.Instance.HasResource("OberthEffect/Fuel"));
		Debug.Log(VehicleResourceDatabase.Instance.HasResource("OberthEffect/Energy"));
		Debug.Log(VehicleResourceDatabase.Instance.HasResource("Blah"));
	}
}
}