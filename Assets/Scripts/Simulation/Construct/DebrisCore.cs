using System;
using System.Linq;
using Photon.Pun;
using Syy1125.OberthEffect.Blocks;
using Syy1125.OberthEffect.Common.ColorScheme;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[Serializable]
public struct DebrisBlockInfo
{
	public Vector2Int Position;
	public string DebrisState;
}

[Serializable]
public struct DebrisInfo
{
	public int OriginViewId;
	public DebrisBlockInfo[] Blocks;
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

		 origin.GetComponent<ConstructBlockManager>().TransferDebrisBlocksTo(
			GetComponent<ConstructBlockManager>(), debrisInfo.Blocks
		);
	}
}
}