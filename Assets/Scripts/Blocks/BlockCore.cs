using System;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public interface IBlockCoreRegistry : IBlockRegistry<BlockCore>
{}

public class BlockCore : MonoBehaviour
{
	public bool IsMine { get; private set; }

	[NonSerialized]
	public string BlockId;
	[NonSerialized]
	public Vector2Int RootPosition;
	[NonSerialized]
	public int Rotation;
	[NonSerialized]
	public Vector2 CenterOfMassPosition;

	private void Awake()
	{
		var photonView = GetComponentInParent<PhotonView>();
		IsMine = photonView == null || photonView.IsMine;
	}

	private void OnEnable()
	{
		GetComponentInParent<IBlockCoreRegistry>()?.RegisterBlock(this);
	}

	private void OnDisable()
	{
		GetComponentInParent<IBlockCoreRegistry>()?.UnregisterBlock(this);
	}
}
}