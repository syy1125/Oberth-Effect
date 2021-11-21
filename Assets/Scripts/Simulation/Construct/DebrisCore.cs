using System;
using System.Collections.Generic;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common.ColorScheme;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[Serializable]
public struct DebrisInfo
{
	public int OriginViewId;
	public Vector2Int[] Positions;
}

public class DebrisCore : MonoBehaviour, IPunInstantiateMagicCallback
{
	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		GetComponent<GlowReferenceTransform>().enabled = false;
		DebrisInfo debrisInfo = JsonUtility.FromJson<DebrisInfo>((string) info.photonView.InstantiationData[0]);

		PhotonView origin = PhotonView.Find(debrisInfo.OriginViewId);
		if (origin == null)
		{
			Debug.LogError($"Debris got origin id {debrisInfo.OriginViewId} but PhotonView.Find returned null!");
			return;
		}

		IEnumerable<GameObject> debrisBlocks = origin.GetComponent<ConstructBlockManager>().TransferBlocksTo(
			GetComponent<ConstructBlockManager>(), debrisInfo.Positions
		);

		foreach (GameObject block in debrisBlocks)
		{
			foreach (IHasDebrisLogic component in block.GetComponents<IHasDebrisLogic>())
			{
				component.EnterDebrisMode();
			}
		}
	}
}
}