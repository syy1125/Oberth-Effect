using UnityEngine;

namespace Syy1125.OberthEffect.Common.UserInterface
{
// Some UI operations involve enabling and disabling panels. This breaks sound emitters as they stop making sound when disabled.
// To deal with this, have a persistent sound source that doesn't go away when you enable/disable other panels.
[RequireComponent(typeof(AudioSource))]
public class UISoundManager : MonoBehaviour
{
	public static UISoundManager Instance { get; private set; }
	
	private AudioSource _audio;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		_audio = GetComponent<AudioSource>();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void PlaySound(AudioClip clip)
	{
		_audio.PlayOneShot(clip);
	}
}
}