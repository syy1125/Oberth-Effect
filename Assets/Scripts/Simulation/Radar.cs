﻿using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Lib.Utils;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.Simulation.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Simulation
{
public class Radar : MonoBehaviour
{
	public Rigidbody2D OwnVehicle;
	public GameObject RadarPingPrefab;
	public Sprite VehiclePingSprite;
	public Sprite ShipyardPingSprite;
	public InputActionReference ZoomAction;
	public float ZoomInterval = 0.4f;

	public float[] Scales;
	public int ScaleIndex;

	public RectTransform RulerTransform;
	public Text RulerText;

	private float _scale;
	private float _scaleVelocity;
	private float _rulerUnit;
	private float _rulerUnitVelocity;

	private Coroutine _zoomIn;
	private Coroutine _zoomOut;
	private List<GameObject> _pings;

	private void Awake()
	{
		_pings = new List<GameObject>();
		_scale = Scales[ScaleIndex];
		_rulerUnit = GetTargetRulerUnit();
	}
	private void LateUpdate()
	{
		if (OwnVehicle == null) return;

		UpdateScale();
		UpdateRuler();
		UpdatePings();
	}

	private void UpdateScale()
	{
		float zoomInput = ZoomAction.action.ReadValue<float>();

		if (zoomInput > 0)
		{
			if (_zoomIn == null) _zoomIn = StartCoroutine(DoZoom(1));
			if (_zoomOut != null)
			{
				StopCoroutine(_zoomOut);
				_zoomOut = null;
			}
		}
		else if (zoomInput < 0)
		{
			if (_zoomOut == null) _zoomOut = StartCoroutine(DoZoom(-1));
			if (_zoomIn != null)
			{
				StopCoroutine(_zoomIn);
				_zoomIn = null;
			}
		}
		else
		{
			if (_zoomIn != null)
			{
				StopCoroutine(_zoomIn);
				_zoomIn = null;
			}

			if (_zoomOut != null)
			{
				StopCoroutine(_zoomOut);
				_zoomOut = null;
			}
		}

		_scale = Mathf.SmoothDamp(_scale, Scales[ScaleIndex], ref _scaleVelocity, ZoomInterval / 2f);
	}

	private void UpdateRuler()
	{
		_rulerUnit = Mathf.SmoothDamp(_rulerUnit, GetTargetRulerUnit(), ref _rulerUnitVelocity, ZoomInterval / 2f);
		Vector2 offsetMax = RulerTransform.offsetMax;
		offsetMax.x = _rulerUnit * _scale;
		RulerTransform.offsetMax = offsetMax;
		(float value, string prefix) = UnitUtils.GetMetricPrefix(_rulerUnit * PhysicsConstants.METERS_PER_UNIT_LENGTH);
		RulerText.text = $"{value:G2}{prefix}m";
	}

	private void UpdatePings()
	{
		int i = 0;
		Vector2 centerOfMass = OwnVehicle.worldCenterOfMass;

		foreach (VehicleCore vehicle in VehicleCore.ActiveVehicles)
		{
			if (vehicle == null) continue;

			Vector2 relativePosition = vehicle.GetComponent<Rigidbody2D>().worldCenterOfMass - centerOfMass;

			GameObject ping = GetOrCreatePing(i);

			ping.transform.localPosition = relativePosition * _scale;
			ping.transform.rotation = vehicle.transform.rotation;
			ping.GetComponent<Image>().sprite = VehiclePingSprite;
			ping.GetComponent<Image>().color =
				PhotonTeamManager.GetPlayerTeamColor(vehicle.GetComponent<PhotonView>().Owner);

			i++;
		}

		foreach (Shipyard shipyard in Shipyard.ActiveShipyards.Values)
		{
			if (shipyard == null) continue;

			Vector2 relativePosition = (Vector2) shipyard.transform.position - centerOfMass;

			GameObject ping = GetOrCreatePing(i);

			ping.transform.localPosition = relativePosition * _scale;
			ping.transform.rotation = Quaternion.identity;
			ping.GetComponent<Image>().sprite = ShipyardPingSprite;
			ping.GetComponent<Image>().color = PhotonTeamManager.GetTeamColor(shipyard.TeamIndex);

			i++;
		}

		for (; i < _pings.Count; i++)
		{
			_pings[i].SetActive(false);
		}
	}

	private float GetTargetRulerUnit()
	{
		float unit = 100;
		while (unit * _scale < 50) unit *= 10;
		while (unit * _scale > 500) unit /= 10;

		// 250..500 -> 50..100
		if (unit * _scale > 250) unit *= 0.2f;
		// 100..250 -> 50..125
		else if (unit * _scale > 100) unit *= 0.5f;

		return unit;
	}

	private GameObject GetOrCreatePing(int i)
	{
		if (i < _pings.Count)
		{
			GameObject ping = _pings[i];
			ping.SetActive(true);
			return ping;
		}
		else
		{
			GameObject ping = Instantiate(RadarPingPrefab, transform);
			_pings.Add(ping);
			return ping;
		}
	}

	private IEnumerator DoZoom(int direction)
	{
		while (true)
		{
			ScaleIndex = Mathf.Clamp(ScaleIndex + direction, 0, Scales.Length - 1);
			yield return new WaitForSeconds(ZoomInterval);
		}
	}
}
}