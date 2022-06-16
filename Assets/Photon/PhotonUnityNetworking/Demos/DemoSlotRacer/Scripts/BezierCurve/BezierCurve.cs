// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BezierCurve.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Networking Demos
// </copyright>
// <summary>
//  Original: http://catlikecoding.com/unity/tutorials/curves-and-splines/
//  Used in SlotRacer Demo
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;

namespace Photon.Pun.Demo.SlotRacer.Utils
{
	public class BezierCurve : MonoBehaviour
	{
		public Vector3[] points;
		
		public Vector3 GetPoint (float t)
		{
			return transform.TransformPoint(Bezier.GetPoint(points[0], points[1], points[2], points[3], t));
		}
		
		public Vector3 GetVelocity (float t)
		{
			return transform.TransformPoint(Bezier.GetFirstDerivative(points[0], points[1], points[2], points[3], t)) - transform.position;
		}
		
		public Vector3 GetDirection (float t)
		{
			return GetVelocity(t).normalized;
		}
		
		public void Reset ()
		{
			points = new Vector3[] {
				new(1f, 0f, 0f),
				new(2f, 0f, 0f),
				new(3f, 0f, 0f),
				new(4f, 0f, 0f)
			};
		}
	}
}