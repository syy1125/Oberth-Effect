using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Common
{
[CreateAssetMenu(menuName = "Scriptable Objects/Vehicle Resource", fileName = "Resource")]
public class VehicleResource : ScriptableObject
{
	public string DisplayName;
	public string ShortName;
	public Color DisplayColor;
}

[Serializable]
public struct ResourceEntry
{
	public VehicleResource Resource;
	public float Amount;
}
}