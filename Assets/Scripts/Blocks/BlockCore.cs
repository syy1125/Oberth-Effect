using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Syy1125.OberthEffect.Blocks
{
public interface IBlockCoreRegistry : IBlockRegistry<BlockCore>, IEventSystemHandler
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
		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.RegisterBlock(this)
		);
	}

	private void OnDisable()
	{
		ExecuteEvents.ExecuteHierarchy<IBlockCoreRegistry>(
			gameObject, null, (handler, _) => handler.UnregisterBlock(this)
		);
	}
}
}