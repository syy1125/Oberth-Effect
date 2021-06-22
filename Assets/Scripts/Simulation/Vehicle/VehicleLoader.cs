using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common;
using Syy1125.OberthEffect.Utils;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Vehicle
{
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleLoader : MonoBehaviourPun, IPunInstantiateMagicCallback
{
	public void SpawnVehicle(VehicleBlueprint blueprint)
	{
		float totalMass = 0f;
		Vector2 centerOfMass = Vector2.zero;
		var momentOfInertiaData = new LinkedList<Tuple<Vector2, float, float>>();

		foreach (VehicleBlueprint.BlockInstance block in blueprint.Blocks)
		{
			GameObject blockPrefab = BlockDatabase.Instance.GetBlock(block.BlockID);

			if (blockPrefab == null)
			{
				Debug.LogError($"Failed to load block by ID: {block.BlockID}");
				continue;
			}

			var rootLocation = new Vector2(block.X, block.Y);
			var rootLocationInt = new Vector2Int(block.X, block.Y);

			GameObject go = Instantiate(blockPrefab, transform);
			go.transform.localPosition = rootLocation;
			go.transform.localRotation = RotationUtils.GetPhysicalRotation(block.Rotation);
			SetLayerRecursively(go, gameObject.layer);

			var info = blockPrefab.GetComponent<BlockInfo>();

			Vector2 blockCenter = rootLocation + RotationUtils.RotatePoint(info.CenterOfMass, block.Rotation);
			totalMass += info.Mass;
			centerOfMass += info.Mass * blockCenter;
			momentOfInertiaData.AddLast(new Tuple<Vector2, float, float>(blockCenter, info.Mass, info.MomentOfInertia));

			var blockCore = go.GetComponent<BlockCore>();
			blockCore.OwnerId = photonView.OwnerActorNr;
			blockCore.RootLocation = rootLocationInt;
		}

		if (totalMass > Mathf.Epsilon)
		{
			centerOfMass /= totalMass;
		}

		var momentOfInertia = 0f;
		foreach (Tuple<Vector2, float, float> blockData in momentOfInertiaData)
		{
			(Vector2 position, float mass, float blockMoment) = blockData;
			momentOfInertia += blockMoment + mass * (position - centerOfMass).sqrMagnitude;
		}

		var body = GetComponent<Rigidbody2D>();
		body.mass = totalMass;
		body.centerOfMass = centerOfMass;
		body.inertia = momentOfInertia;

		transform.position -= (Vector3) centerOfMass;
	}

	private static void SetLayerRecursively(GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform child in go.transform)
		{
			SetLayerRecursively(child.gameObject, layer);
		}
	}

	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;
		var blueprint = JsonUtility.FromJson<VehicleBlueprint>((string) instantiationData[0]);
		SpawnVehicle(blueprint);
	}
}
}