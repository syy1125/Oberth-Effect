using System;
using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class DesignerCursorTexture : MonoBehaviour
{
	public enum CursorStatus
	{
		Default,
		Drag,
		Eraser
	}


	[Header("Cursor Textures")]
	public Texture2D GrabTexture;
	public Texture2D EraserTexture;

	[NonSerialized]
	public CursorStatus TargetStatus;
	private CursorStatus _status;

	private void LateUpdate()
	{
		if (TargetStatus != _status)
		{
			switch (TargetStatus)
			{
				case CursorStatus.Default:
					Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
					break;
				case CursorStatus.Drag:
					Cursor.SetCursor(GrabTexture, new Vector2(50f, 50f), CursorMode.Auto);
					break;
				case CursorStatus.Eraser:
					Cursor.SetCursor(EraserTexture, new Vector2(10f, 10f), CursorMode.Auto);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			_status = TargetStatus;
		}
	}
}
}