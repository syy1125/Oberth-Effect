using UnityEngine;

namespace Syy1125.OberthEffect.Input
{
public class SuppressActionMap : MonoBehaviour
{
	public string[] DisableMaps;

	private void OnEnable()
	{
		ActionMapControl.Instance.AddDisabledMaps(DisableMaps);
	}

	private void OnDisable()
	{
		if (ActionMapControl.Instance != null)
		{
			ActionMapControl.Instance.RemoveDisabledMaps(DisableMaps);
		}
	}
}
}