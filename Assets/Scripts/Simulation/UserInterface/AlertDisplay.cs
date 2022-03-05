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
	public VehicleWeaponControl WeaponControl;
	public Text MissileCountDisplay;
	public Text MissileTimerDisplay;
	public AudioSource AlertAudio;
	public Transform MissileMarkerParent;
	public GameObject MissileMarkerPrefab;

	private Dictionary<Missile, GameObject> _missileMarkers;

	private void Awake()
	{
		_missileMarkers = new Dictionary<Missile, GameObject>();
	}

	private void LateUpdate()
	{
		UpdateMissileDisplay();
	}

	private void UpdateMissileDisplay()
	{
		if (WeaponControl == null || !WeaponControl.isActiveAndEnabled)
		{
			MissileCountDisplay.gameObject.SetActive(false);
			MissileTimerDisplay.gameObject.SetActive(false);
			AlertAudio.mute = true;
			return;
		}

		if (WeaponControl.IncomingMissiles.Count == 0)
		{
			MissileCountDisplay.gameObject.SetActive(false);
			MissileTimerDisplay.gameObject.SetActive(false);
			AlertAudio.mute = true;

			foreach (GameObject marker in _missileMarkers.Values)
			{
				Destroy(marker);
			}

			_missileMarkers.Clear();
		}
		else
		{
			Color textColor = Time.unscaledTime % 1 >= 0.5f ? Color.red : Color.white;

			MissileCountDisplay.gameObject.SetActive(true);
			MissileCountDisplay.text = WeaponControl.IncomingMissiles.Count > 1
				? $"< {WeaponControl.IncomingMissiles.Count} Incoming Missiles >"
				: "< Incoming Missile >";
			MissileCountDisplay.color = textColor;
			AlertAudio.mute = false;


			HashSet<Missile> missiles = new HashSet<Missile>(WeaponControl.IncomingMissiles);
			float? minTime = missiles.Select(missile => missile.GetHitTime())
				.Where(hitTime => hitTime != null && hitTime > 0f)
				.Min();

			if (minTime != null)
			{
				MissileTimerDisplay.gameObject.SetActive(true);
				MissileTimerDisplay.text = $"< Time to Impact {minTime.Value:0.00}s >";
				MissileTimerDisplay.color = textColor;

				AlertAudio.pitch = minTime.Value < 1.5f ? 1.4f : 1f;
			}
			else
			{
				MissileTimerDisplay.gameObject.SetActive(false);

				AlertAudio.pitch = 1f;
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
				_missileMarkers.Add(missile, marker);
			}
		}
	}
}
}