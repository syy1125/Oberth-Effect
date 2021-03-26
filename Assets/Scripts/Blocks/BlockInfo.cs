using System;
using UnityEngine;

public class BlockInfo : MonoBehaviour
{
	[Header("Core Data")]
	public bool ShowInDesigner;

	public string BlockID;

	public string ShortName;
	public string FullName;

	[Header("Physics")]
	public BoundsInt Bounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);

	public float Mass;
	public float MomentOfInertia;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(Bounds.center - new Vector3(0.5f, 0.5f, 0.5f), Bounds.size);
	}
}