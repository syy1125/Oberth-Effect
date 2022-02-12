using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Foundation.UserInterface
{
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
	public AudioClip Sound;
	public float VolumeScale = 1f;

	private void OnEnable()
	{
		GetComponent<Button>().onClick.AddListener(PlaySound);
	}

	private void OnDisable()
	{
		GetComponent<Button>().onClick.RemoveListener(PlaySound);
	}

	private void PlaySound()
	{
		if (Sound != null)
		{
			UISoundManager.Instance.PlaySound(Sound, VolumeScale);
		}
	}
}
}