using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Common.Utils;
using Syy1125.OberthEffect.Simulation.Construct;
using Syy1125.OberthEffect.Spec.Database;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Syy1125.OberthEffect.Simulation.Game
{
public class ResourceDepot : MonoBehaviour
{
	[Serializable]
	public struct ResourceEntry
	{
		public string Resource;
		public float Amount;
	}

	public int TeamIndex;
	public float ResupplyRange;
	public List<ResourceEntry> ResourceEmissionRate;

	public GameObject ResourceParticlePrefab;
	public float BaseParticleRate = 20f;

	private float _sqrRange;
	private Dictionary<string, float> _transferResources;

	private Dictionary<GameObject, GameObject> _vehicleToEmitter;
	private ParticleSystem.EmitParams _supplyParticleParams;

	private void Awake()
	{
		_sqrRange = ResupplyRange * ResupplyRange;
		_transferResources = new Dictionary<string, float>();
		_vehicleToEmitter = new Dictionary<GameObject, GameObject>();
		_supplyParticleParams = new ParticleSystem.EmitParams();
	}

	private void FixedUpdate()
	{
		foreach (VehicleCore vehicle in VehicleCore.ActiveVehicles)
		{
			if (vehicle == null) continue;
			Player owner = vehicle.GetComponent<PhotonView>().Owner;
			if (PhotonTeamManager.GetPlayerTeamIndex(owner) != TeamIndex) continue;

			Vector2 relativePosition = vehicle.transform.position - transform.position;
			if (relativePosition.sqrMagnitude > _sqrRange) continue;

			if (!_vehicleToEmitter.TryGetValue(vehicle.gameObject, out GameObject emitter))
			{
				emitter = Instantiate(ResourceParticlePrefab, transform);
				emitter.transform.localPosition = Vector3.zero;
				_vehicleToEmitter.Add(vehicle.gameObject, emitter);
			}

			VehicleResourceManager resourceManager = vehicle.GetComponent<VehicleResourceManager>();
			_transferResources.Clear();
			foreach (var entry in ResourceEmissionRate)
			{
				var resourceStatus = resourceManager.GetResourceStatus(entry.Resource);
				if (resourceStatus != null)
				{
					float supplyAmount = entry.Amount * Time.fixedDeltaTime;
					float particleRate = BaseParticleRate;

					float remainingCapacity = resourceStatus.StorageCapacity - resourceStatus.CurrentAmount;
					if (remainingCapacity < supplyAmount)
					{
						particleRate *= remainingCapacity / supplyAmount;
						supplyAmount = remainingCapacity;
					}

					if (!Mathf.Approximately(supplyAmount, 0f))
					{
						_transferResources.Add(entry.Resource, supplyAmount);

						var resourceSpec = VehicleResourceDatabase.Instance.GetResourceSpec(entry.Resource).Spec;
						_supplyParticleParams.startColor = resourceSpec.GetDisplayColor();
						float particleCount = particleRate * Time.fixedDeltaTime;
						int count = Mathf.FloorToInt(particleCount);
						if (Random.value < particleCount - count) count++;
						emitter.GetComponent<ParticleSystem>().Emit(_supplyParticleParams, count);
					}
				}
			}

			if (_transferResources.Count > 0)
			{
				resourceManager.AddResources(_transferResources);
			}
		}
	}

	private void Update()
	{
		List<GameObject> nullVehicles = new List<GameObject>();

		foreach (KeyValuePair<GameObject, GameObject> entry in _vehicleToEmitter)
		{
			if (entry.Key == null)
			{
				nullVehicles.Add(entry.Key);
				continue;
			}

			entry.Value.GetComponent<ConvergingParticleEmitter>().Target = entry.Key.transform.position;
		}

		foreach (GameObject nullVehicle in nullVehicles)
		{
			_vehicleToEmitter.Remove(nullVehicle);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, ResupplyRange);
	}
}
}