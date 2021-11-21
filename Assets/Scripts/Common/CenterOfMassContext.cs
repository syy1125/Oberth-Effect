using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
[RequireComponent(typeof(Rigidbody2D))]
public class CenterOfMassContext : MonoBehaviour
{
	public virtual Vector2 GetCenterOfMass()
	{
		return GetComponent<Rigidbody2D>().centerOfMass;
	}
}
}