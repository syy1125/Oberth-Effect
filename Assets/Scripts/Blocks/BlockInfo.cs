﻿using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
public class BlockInfo : MonoBehaviour
{
	[Header("Core Data")]
	public bool ShowInDesigner;

	public bool AllowErase;

	public string BlockID;

	public string ShortName;
	public string FullName;

	[Header("Stats")]
	public float MaxHealth;

	public float IntegrityScore;

	[Header("Construction")]
	public BoundsInt Bounds = new BoundsInt(Vector3Int.zero, Vector3Int.one);

	public Vector2Int[] AttachmentPoints;

	[Header("Physics")]
	public Vector2 CenterOfMass;

	public float Mass;
	public float MomentOfInertia;

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.identity;

		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(Bounds.center - new Vector3(0.5f, 0.5f, 0.5f), Bounds.size);

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(CenterOfMass, MomentOfInertia * 6);

		if (AttachmentPoints != null)
		{
			Gizmos.color = Color.yellow;
			foreach (Vector2Int point in AttachmentPoints)
			{
				Gizmos.DrawIcon(new Vector3(point.x, point.y), "CrossIcon");
			}
		}
	}
}
}