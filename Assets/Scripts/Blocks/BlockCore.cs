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
	public int OwnerId { get; set; }
	public bool IsMine { get; private set; }
	public string BlockId { get; set; }
	public Vector2Int RootPosition { get; set; }
	public int Rotation { get; set; }

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