using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Foundation.Match;
using Syy1125.OberthEffect.Lib.Math;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class PhotonTimeTest : MonoBehaviourPunCallbacks
{
	public Orbit2D TestOrbit;

	// private void Start()
	// {
	// 	PhotonNetwork.ConnectUsingSettings();
	// }
	//
	// public override void OnConnectedToMaster()
	// {
	// 	PhotonNetwork.JoinLobby();
	// }
	//
	// public override void OnJoinedLobby()
	// {
	// 	PhotonNetwork.CreateRoom("test");
	// }

	// private void FixedUpdate()
	// {
	// 	if (PhotonNetwork.CurrentRoom == null) return;
	// 	(Vector2 position, Vector2 _) = TestOrbit.GetStateVectorAt((float) PhotonNetwork.Time);
	// 	GetComponent<Rigidbody2D>().MovePosition(position);
	// }

	private void OnDrawGizmos()
	{
		(Vector2 position, Vector2 velocity) = TestOrbit.GetStateVectorAt(Time.time);
		Gizmos.color = Color.magenta;
		transform.position = position;
		Gizmos.DrawLine(position, position + velocity);
	}
}
}