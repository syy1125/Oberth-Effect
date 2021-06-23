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

			var blockCore = go.GetComponent<BlockCore>();
			blockCore.OwnerId = photonView.OwnerActorNr;
			blockCore.Initialize(rootLocationInt, block.Rotation);
		}
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