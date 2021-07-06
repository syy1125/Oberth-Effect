using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
public class CenterOfMassContext : MonoBehaviour
{
	public Rigidbody2D Body;

	public virtual Vector2 GetCenterOfMass()
	{
		return Body.centerOfMass;
	}
}
}