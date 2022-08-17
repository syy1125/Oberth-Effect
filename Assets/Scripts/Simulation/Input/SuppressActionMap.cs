using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Input
{
public class SuppressActionMap : MonoBehaviour
{
	public string[] DisableMaps;

	private void OnEnable()
	{
		ActionMapControl.Instance.AddSuppressedMaps(DisableMaps);
	}

	private void OnDisable()
	{
		if (ActionMapControl.Instance != null)
		{
			ActionMapControl.Instance.RemoveSuppressedMaps(DisableMaps);
		}
	}
}
}