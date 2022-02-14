using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Foundation;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
public class BlockSoundAttenuator : MonoBehaviour, IBlockSoundAttenuator
{
	public float AttenuationRate = 1f;

	private Dictionary<string, float> _persistentAttenuation;
	private Dictionary<string, float> _persistentVolumes;
	private Dictionary<string, float> _oneShotAttenuation;

	private void Awake()
	{
		_persistentAttenuation = new Dictionary<string, float>();
		_persistentVolumes = new Dictionary<string, float>();
		_oneShotAttenuation = new Dictionary<string, float>();
	}

	private void Start()
	{
		StartCoroutine(LateFixedUpdate());
	}

	private IEnumerator LateFixedUpdate()
	{
		while (isActiveAndEnabled)
		{
			yield return new WaitForFixedUpdate();

			float attenuateAmount = AttenuationRate * Time.fixedDeltaTime;

			foreach (KeyValuePair<string, float> entry in _persistentAttenuation.ToList())
			{
				if (_persistentVolumes.TryGetValue(entry.Key, out float volume))
				{
					_persistentAttenuation[entry.Key] = Mathf.Lerp(entry.Value, 1 / (volume + 1), attenuateAmount);
					_persistentVolumes.Remove(entry.Key);
				}
				else
				{
					_persistentAttenuation[entry.Key] = Mathf.Lerp(entry.Value, 1f, attenuateAmount);
				}
			}

			foreach (KeyValuePair<string, float> entry in _persistentVolumes)
			{
				_persistentAttenuation[entry.Key] = Mathf.Lerp(1f, 1 / (entry.Value + 1), attenuateAmount);
			}

			_persistentVolumes.Clear();

			foreach (string soundId in _oneShotAttenuation.Keys.ToList())
			{
				_oneShotAttenuation[soundId] = Mathf.Lerp(_oneShotAttenuation[soundId], 1f, attenuateAmount);
			}
		}
	}

	public float AttenuatePersistentSound(string soundId, float volume)
	{
		if (_persistentVolumes.TryGetValue(soundId, out float savedVolume))
		{
			_persistentVolumes[soundId] = savedVolume + volume;
		}
		else
		{
			_persistentVolumes.Add(soundId, volume);
		}

		return (_persistentAttenuation.TryGetValue(soundId, out float attenuation) ? attenuation : 1f) * volume;
	}

	public float AttenuateOneShotSound(string soundId, float volume)
	{
		if (_oneShotAttenuation.TryGetValue(soundId, out float attenuation))
		{
			_oneShotAttenuation[soundId] /= 1 + volume;
			return attenuation * volume;
		}
		else
		{
			_oneShotAttenuation[soundId] = 1 / (1 + volume);
			return volume;
		}
	}
}
}