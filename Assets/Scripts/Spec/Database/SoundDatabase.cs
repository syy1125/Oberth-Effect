using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Syy1125.OberthEffect.Spec.ModLoading;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace Syy1125.OberthEffect.Spec.Database
{
public class SoundDatabase : MonoBehaviour, IGameContentDatabase
{
	public static SoundDatabase Instance { get; private set; }

	public AudioMixerGroup BlockSoundGroup;

	private Dictionary<string, SpecInstance<SoundSpec>> _specs;
	private Dictionary<string, AudioClip> _sounds;

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
	}

	public void Reload()
	{
		_specs = ModLoader.SoundPipeline.Results
			.ToDictionary(instance => instance.Spec.SoundId, instance => instance);

		if (_sounds != null)
		{
			foreach (AudioClip clip in _sounds.Values)
			{
				clip.UnloadAudioData();
				Destroy(clip);
			}
		}

		_sounds = new Dictionary<string, AudioClip>();
		Debug.Log($"Loaded {_specs.Count} sound specs");
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;

			if (_sounds != null)
			{
				foreach (AudioClip clip in _sounds.Values)
				{
					clip.UnloadAudioData();
					Destroy(clip);
				}
			}
		}
	}

	public IEnumerator LoadAudioClips()
	{
		foreach (string soundId in _specs.Keys)
		{
			if (!_sounds.ContainsKey(soundId))
			{
				yield return StartCoroutine(LoadAudioClip(soundId));
			}
		}
	}

	public bool ContainsId(string soundId)
	{
		return soundId != null && _specs.ContainsKey(soundId);
	}

	private IEnumerator LoadAudioClip(string soundId)
	{
		var instance = _specs[soundId];

		// Reference: https://learn.unity.com/tutorial/working-with-the-streamingassets-folder-2019-2#5ff39515edbc2a4fd922da9b
		string fileUri = "file://" + instance.Spec.SoundPath;

		AudioType audioType = Path.GetExtension(instance.Spec.SoundPath) switch
		{
			".wav" => AudioType.WAV,
			_ => AudioType.UNKNOWN
		};

		using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType);

		yield return request.SendWebRequest();

		_sounds[soundId] = DownloadHandlerAudioClip.GetContent(request);
	}

	public AudioClip GetAudioClip(string soundId)
	{
		return _sounds.TryGetValue(soundId, out AudioClip clip) ? clip : null;
	}

	public AudioSource CreateBlockAudioSource(GameObject go, bool attenuate)
	{
		var audioSource = go.AddComponent<AudioSource>();
		audioSource.outputAudioMixerGroup = BlockSoundGroup;
		audioSource.spatialBlend = attenuate ? 0.9f : 0f;
		return audioSource;
	}
}
}