using System.Collections.Generic;
using System.Linq;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation.UserInterface
{
public class AlertDisplay : MonoBehaviour
{
	private enum AlertState
	{
		Idle, // No missile, or missile is far away
		MissileClose, // Missile is within a few seconds of impact
		MissileImminent // Missile is about to hit
	}

	private VehicleWeaponControl _weaponControl;

	public VehicleWeaponControl WeaponControl
	{
		get => _weaponControl;
		set
		{
			if (_weaponControl == value) return;

			if (_weaponControl != null)
			{
				_weaponControl.OnIncomingMissileAdded.RemoveListener(AlertMissileLaunch);
			}

			_weaponControl = value;

			if (_weaponControl != null)
			{
				_weaponControl.OnIncomingMissileAdded.AddListener(AlertMissileLaunch);
			}
		}
	}

	public Text MissileCountDisplay;
	public Text MissileTimerDisplay;
	public AudioSource AlertAudio;
	public AudioSource SirenAudio;
	public Transform MissileMarkerParent;
	public GameObject MissileMarkerPrefab;

	private AlertState _alertState;
	private Dictionary<Missile, GameObject> _missileMarkers;

	private void Awake()
	{
		_missileMarkers = new Dictionary<Missile, GameObject>();
	}

	private void AlertMissileLaunch()
	{
		if (_alertState == AlertState.Idle && !AlertAudio.isPlaying)
		{
			AlertAudio.Play();
		}
	}

	private void LateUpdate()
	{
		UpdateMissileDisplay();
		UpdateMissileAlertSound();
	}

	private void UpdateMissileDisplay()
	{
		if (WeaponControl == null || !WeaponControl.isActiveAndEnabled || WeaponControl.IncomingMissiles.Count == 0)
		{
			MissileCountDisplay.gameObject.SetActive(false);
			MissileTimerDisplay.gameObject.SetActive(false);
			_alertState = AlertState.Idle;

			foreach (GameObject marker in _missileMarkers.Values)
			{
				Destroy(marker);
			}

			_missileMarkers.Clear();

			return;
		}

		Color textColor = Time.unscaledTime % 1 >= 0.5f ? Color.red : Color.white;

		MissileCountDisplay.gameObject.SetActive(true);
		MissileCountDisplay.text = WeaponControl.IncomingMissiles.Count > 1
			? $"< {WeaponControl.IncomingMissiles.Count} Incoming Missiles >"
			: "< Incoming Missile >";
		MissileCountDisplay.color = textColor;

		// Use HashSet for efficient removal when calculating missile markers
		HashSet<Missile> missiles = new HashSet<Missile>(WeaponControl.IncomingMissiles);
		float? minTime = missiles.Select(missile => missile.GetHitTime())
			.Where(hitTime => hitTime != null && hitTime > 0f)
			.Min();

		if (minTime != null)
		{
			MissileTimerDisplay.gameObject.SetActive(true);
			MissileTimerDisplay.text = $"< Time to Impact {minTime.Value:0.00}s >";
			MissileTimerDisplay.color = textColor;

			if (minTime.Value > 4f)
			{
				_alertState = AlertState.Idle;
			}
			else if (minTime.Value > 1.5f)
			{
				_alertState = AlertState.MissileClose;
			}
			else
			{
				_alertState = AlertState.MissileImminent;
			}
		}
		else
		{
			MissileTimerDisplay.gameObject.SetActive(false);
			_alertState = AlertState.Idle;
		}

		List<Missile> removedKeys = new List<Missile>();
		foreach (KeyValuePair<Missile, GameObject> entry in _missileMarkers)
		{
			if (!missiles.Remove(entry.Key))
			{
				removedKeys.Add(entry.Key);
			}
		}

		foreach (Missile missile in removedKeys)
		{
			Destroy(_missileMarkers[missile]);
			_missileMarkers.Remove(missile);
		}

		foreach (Missile missile in missiles)
		{
			var marker = Instantiate(MissileMarkerPrefab, MissileMarkerParent);
			marker.GetComponent<HighlightTarget>().Target = missile.transform;
			marker.GetComponent<HighlightTarget>().Init();
			_missileMarkers.Add(missile, marker);
		}
	}

	private void UpdateMissileAlertSound()
	{
		switch (_alertState)
		{
			case AlertState.Idle:
				SirenAudio.mute = true;
				break;
			case AlertState.MissileClose:
				SirenAudio.mute = false;
				SirenAudio.pitch = 1f;
				AlertAudio.Stop();
				break;
			case AlertState.MissileImminent:
				SirenAudio.mute = false;
				SirenAudio.pitch = 1.4f;
				AlertAudio.Stop();
				break;
		}
	}
}
}