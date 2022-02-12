using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Foundation.UserInterface
{
[RequireComponent(typeof(Toggle))]
public class ToggleSound : MonoBehaviour
{
	public AudioClip Sound;
	public float VolumeScale = 1f;

	private void OnEnable()
	{
		GetComponent<Toggle>().onValueChanged.AddListener(PlaySound);
	}

	private void OnDisable()
	{
		GetComponent<Toggle>().onValueChanged.RemoveListener(PlaySound);
	}

	private void PlaySound(bool _)
	{
		var context = GetComponentInParent<IElementControllerContext>();

		if (Sound != null && !(context is { UpdatingElements: true }))
		{
			UISoundManager.Instance.PlaySound(Sound, VolumeScale);
		}
	}
}
}