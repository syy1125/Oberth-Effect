using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class BlockIndicators : MonoBehaviour
{
	public Sprite DisconnectSprite;
	public Color DisconnectColor = Color.yellow;
	public Material DisconnectMaterial;

	public Sprite ConflictSprite;
	public Color ConflictColor = Color.red;
	public Material ConflictMaterial;

	public Sprite AttachedBlockSprite;
	public Color AttachedBlockColor = Color.green;
	public Material AttachedBlockMaterial;

	public Sprite ClosedAttachPointSprite;
	public Color ClosedAttachPointColor = Color.gray;
	public Material ClosedAttachPointMaterial;

	public Sprite OpenAttachPointSprite;
	public Color OpenAttachPointColor = Color.white;
	public Material OpenAttachPointMaterial;


	private HashSet<Vector2Int> _conflicts;
	private HashSet<Vector2Int> _disconnects;
	private HashSet<Vector2Int> _attachedBlocks;
	private HashSet<Vector2Int> _closedAttachPoints;
	private HashSet<Vector2Int> _openAttachPoints;
	private Dictionary<Vector2Int, GameObject> _visualObjects;
	private bool _changed;

	private void Awake()
	{
		_conflicts = new HashSet<Vector2Int>();
		_disconnects = new HashSet<Vector2Int>();
		_attachedBlocks = new HashSet<Vector2Int>();
		_closedAttachPoints = new HashSet<Vector2Int>();
		_openAttachPoints = new HashSet<Vector2Int>();

		_visualObjects = new Dictionary<Vector2Int, GameObject>();
		_changed = false;
	}

	public void SetConflicts(IEnumerable<Vector2Int> conflicts)
	{
		if (UpdateSet(_conflicts, conflicts))
		{
			_changed = true;
		}
	}

	public void SetDisconnections(IEnumerable<Vector2Int> disconnects)
	{
		if (UpdateSet(_disconnects, disconnects))
		{
			_changed = true;
		}
	}

	public void SetAttachmentPoints(
		IEnumerable<Vector2Int> attachedBlocks, IEnumerable<Vector2Int> closedAttachPoints,
		IEnumerable<Vector2Int> openAttachPoints
	)
	{
		if (UpdateSet(_attachedBlocks, attachedBlocks))
		{
			_changed = true;
		}

		if (UpdateSet(_closedAttachPoints, closedAttachPoints))
		{
			_changed = true;
		}

		if (UpdateSet(_openAttachPoints, openAttachPoints))
		{
			_changed = true;
		}
	}

	private static bool UpdateSet<T>(ISet<T> current, IEnumerable<T> future)
	{
		HashSet<T> newSet = future == null ? new HashSet<T>() : new HashSet<T>(future);

		if (!current.SetEquals(newSet))
		{
			current.Clear();
			current.UnionWith(newSet);
			return true;
		}
		else
		{
			return false;
		}
	}

	private struct IndicatorItem
	{
		public HashSet<Vector2Int> Positions;

		public Sprite Sprite;
		public Color Color;
		public Material Material;
	}

	public void Update()
	{
		if (!_changed) return;

		var colorQueue = new Queue<IndicatorItem>();
		colorQueue.Enqueue(
			new IndicatorItem
			{
				Positions = new HashSet<Vector2Int>(_attachedBlocks),
				Sprite = AttachedBlockSprite,
				Color = AttachedBlockColor,
				Material = AttachedBlockMaterial
			}
		);
		colorQueue.Enqueue(
			new IndicatorItem
			{
				Positions = new HashSet<Vector2Int>(_closedAttachPoints),
				Sprite = ClosedAttachPointSprite,
				Color = ClosedAttachPointColor,
				Material = ClosedAttachPointMaterial
			}
		);
		colorQueue.Enqueue(
			new IndicatorItem
			{
				Positions = new HashSet<Vector2Int>(_openAttachPoints),
				Sprite = OpenAttachPointSprite,
				Color = OpenAttachPointColor,
				Material = OpenAttachPointMaterial
			}
		);
		colorQueue.Enqueue(
			new IndicatorItem
			{
				Positions = new HashSet<Vector2Int>(_conflicts),
				Sprite = ConflictSprite,
				Color = ConflictColor,
				Material = ConflictMaterial
			}
		);
		colorQueue.Enqueue(
			new IndicatorItem
			{
				Positions = new HashSet<Vector2Int>(_disconnects),
				Sprite = DisconnectSprite,
				Color = DisconnectColor,
				Material = DisconnectMaterial
			}
		);

		// Change color of existing objects
		foreach (KeyValuePair<Vector2Int, GameObject> entry in _visualObjects.ToArray())
		{
			bool found = false;

			foreach (IndicatorItem item in colorQueue)
			{
				if (item.Positions.Remove(entry.Key) && !found)
				{
					var spriteRenderer = entry.Value.GetComponent<SpriteRenderer>();
					spriteRenderer.sprite = item.Sprite;
					spriteRenderer.color = item.Color;
					spriteRenderer.sharedMaterial = item.Material;

					found = true;
				}
			}

			if (!found)
			{
				Destroy(entry.Value);
				_visualObjects.Remove(entry.Key);
			}
		}

		// Instantiate ones that don't exist yet
		while (colorQueue.Count > 0)
		{
			var item = colorQueue.Dequeue();

			foreach (Vector2Int position in item.Positions)
			{
				GameObject visualObject = new GameObject("Visual");
				Transform visualTransform = visualObject.transform;

				visualTransform.SetParent(transform);
				visualTransform.localPosition = new Vector3(position.x, position.y);

				var spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
				spriteRenderer.sprite = item.Sprite;
				spriteRenderer.color = item.Color;
				spriteRenderer.sharedMaterial = item.Material;

				_visualObjects.Add(position, visualObject);

				foreach (IndicatorItem rest in colorQueue)
				{
					rest.Positions.Remove(position);
				}
			}
		}

		_changed = false;
	}
}
}