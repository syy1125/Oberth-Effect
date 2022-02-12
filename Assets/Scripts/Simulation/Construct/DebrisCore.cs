using System;
using Photon.Pun;
using Syy1125.OberthEffect.Foundation.Colors;
using Syy1125.OberthEffect.Lib.Utils;
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

[RequireComponent(typeof(Rigidbody2D))]
public class DebrisCore : MonoBehaviour, IPunInstantiateMagicCallback
{
	public void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		object[] instantiationData = info.photonView.InstantiationData;

		GetComponent<GlowReferenceTransform>().enabled = false;
		DebrisInfo debrisInfo =
			JsonUtility.FromJson<DebrisInfo>(CompressionUtils.Decompress((byte[]) instantiationData[0]));

		PhotonView origin = PhotonView.Find(debrisInfo.OriginViewId);

		if (origin == null)
		{
			Debug.LogError($"Debris got origin id {debrisInfo.OriginViewId} but PhotonView.Find returned null!");
			return;
		}

		origin.GetComponent<ConstructBlockManager>().TransferDebrisBlocksTo(
			GetComponent<ConstructBlockManager>(), debrisInfo.Blocks
		);

		var body = GetComponent<Rigidbody2D>();
		var originBody = origin.GetComponent<Rigidbody2D>();

		body.velocity = originBody.velocity;
		body.angularVelocity = originBody.angularVelocity;
	}
}
}