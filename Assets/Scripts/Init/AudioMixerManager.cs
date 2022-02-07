using System.Collections.Generic;
using Syy1125.OberthEffect.Common;
using UnityEngine;
using UnityEngine.Audio;

namespace Syy1125.OberthEffect.Init
{
public class AudioMixerManager : MonoBehaviour
{
	public static AudioMixerManager Instance { get; private set; }

	public AudioMixer Mixer;

	private Dictionary<string, float> _volumes;

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

		_volumes = new Dictionary<string, float>();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void LoadVolumes()
	{
		LoadVolume(PropertyKeys.MASTER_VOLUME, 0.5f);
		LoadVolume(PropertyKeys.UI_VOLUME, 0.5f);
		LoadVolume(PropertyKeys.BLOCKS_VOLUME, 1f);
	}

	private void LoadVolume(string propertyName, float defaultValue)
	{
		float volume = PlayerPrefs.GetFloat(propertyName, defaultValue);
		Debug.Log($"Volume \"{propertyName}\" is {volume}");
		SetVolume(propertyName, volume, false);
	}

	public float GetVolume(string propertyName)
	{
		return _volumes[propertyName];
	}

	public void SetVolume(string propertyName, float volume, bool save = true)
	{
		_volumes[propertyName] = volume;
		Mixer.SetFloat(propertyName, Mathf.Log10(Mathf.Clamp(volume, 1e-4f, 1f)) * 20);

		if (save)
		{
			PlayerPrefs.SetFloat(propertyName, volume);
		}
	}
}
}