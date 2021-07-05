﻿using Syy1125.OberthEffect.Common;
using UnityEngine;

namespace Syy1125.OberthEffect.Blocks
{
/// <summary>
/// Contains static information about the block
/// </summary>
public class BlockInfo : MonoBehaviour, ITooltipProvider
{
	[Header("Core Data")]
	public string BlockID;
	public string ShortName;
	public string FullName;

	[Header("Designer")]
	public bool ShowInDesigner;
	public bool AllowErase;
	public float PreviewScale = 1f;

	[Header("Stats")]
	public float MaxHealth;
	[Range(1, 10)]
	public float ArmorValue;
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

	public string GetTooltip()
	{
		return string.Join(
			"\n",
			FullName,
			$"  {Mass * PhysicsConstants.KG_PER_UNIT_MASS:0.##} kg, {Bounds.size.x * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m × {Bounds.size.y * PhysicsConstants.METERS_PER_UNIT_LENGTH:F0}m",
			$"  <color=\"red\">{MaxHealth} health</color>, <color=\"lightblue\">{ArmorValue} armor</color>"
		).Trim();
	}
}
}