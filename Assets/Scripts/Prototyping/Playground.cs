using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	private void Start()
	{
		Debug.Log(Mathf.DeltaAngle(0, 90));
		Debug.Log(Mathf.DeltaAngle(90, 0));
	}
}
}