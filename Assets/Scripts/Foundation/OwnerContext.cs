using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Foundation
{
public class OwnerContext : MonoBehaviour
{
	public int OwnerId { get; private set; }

	private void Awake()
	{
		OwnerId = GetComponent<PhotonView>().OwnerActorNr;
	}
}
}