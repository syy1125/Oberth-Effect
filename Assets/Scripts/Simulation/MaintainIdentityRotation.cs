using UnityEngine;

namespace Syy1125.OberthEffect.Simulation
{
public class MaintainIdentityRotation : MonoBehaviour
{
	private void Start()
	{
		transform.rotation = Quaternion.identity;
	}
}
}