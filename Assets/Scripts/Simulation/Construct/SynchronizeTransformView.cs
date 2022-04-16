using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Simulation.Construct
{
[RequireComponent(typeof(Rigidbody2D))]
public class SynchronizeTransformView : MonoBehaviourPun, IPunObservable
{
	private Rigidbody2D _body;

	private void Awake()
	{
		_body = GetComponent<Rigidbody2D>();
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(_body.position);
			stream.SendNext(_body.velocity);
			stream.SendNext(_body.rotation);
			stream.SendNext(_body.angularVelocity);
		}
		else
		{
			// Reference: https://doc.photonengine.com/en-us/pun/current/gameplay/lagcompensation
			_body.position = (Vector2) stream.ReceiveNext();
			_body.velocity = (Vector2) stream.ReceiveNext();
			_body.rotation = (float) stream.ReceiveNext();
			_body.angularVelocity = (float) stream.ReceiveNext();

			float lag = Mathf.Abs((float) (PhotonNetwork.Time - info.SentServerTime));

			_body.position += _body.velocity * lag;
			_body.rotation += _body.angularVelocity * lag;
		}
	}
}
}