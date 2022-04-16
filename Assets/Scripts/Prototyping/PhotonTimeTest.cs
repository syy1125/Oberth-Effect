using Photon.Pun;
using Syy1125.OberthEffect.Foundation;
using Syy1125.OberthEffect.Lib.Math;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class PhotonTimeTest : MonoBehaviourPunCallbacks
{
	public Orbit2D TestOrbit;

	private float _referenceTime;

	private void Start()
	{
		PhotonNetwork.ConnectUsingSettings();
		Camera.main.GetComponent<Rigidbody2D>().velocity = Vector2.right * 20;
	}

	public override void OnConnectedToMaster()
	{
		PhotonNetwork.JoinLobby();
	}

	public override void OnJoinedLobby()
	{
		PhotonNetwork.CreateRoom("test");
	}

	public override void OnJoinedRoom()
	{
		_referenceTime = (float) PhotonNetwork.Time;
		Camera.main.GetComponent<Rigidbody2D>().velocity = Vector2.right * 20;
	}

	private void FixedUpdate()
	{
		if (!PhotonNetwork.InRoom) return;

		// transform.position = new Vector3((float) (PhotonNetwork.Time - _referenceTime) * 20, 4000);
		// transform.position = new Vector3(Time.timeSinceLevelLoad * 20, 4000);
		transform.position = new Vector3(SynchronizedTimer.Instance.SynchronizedTime * 20, 4000);
		// if (PhotonNetwork.CurrentRoom == null) return;
		// (Vector2 position, Vector2 _) = TestOrbit.GetStateVectorAt((float) PhotonNetwork.Time);
		// GetComponent<Rigidbody2D>().MovePosition(position);
	}

	// private void OnDrawGizmos()
	// {
	// 	(Vector2 position, Vector2 velocity) = TestOrbit.GetStateVectorAt(Time.time);
	// 	Gizmos.color = Color.magenta;
	// 	transform.position = position;
	// 	Gizmos.DrawLine(position, position + velocity);
	// }
}
}