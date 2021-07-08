using UnityEngine;

namespace Syy1125.OberthEffect.Designer
{
public class PropulsionCrossSize : MonoBehaviour
{
	public RectTransform Top;
	public RectTransform Bottom;
	public RectTransform Left;
	public RectTransform Right;
	public float Space;

	private RectTransform _transform;

	private void Awake()
	{
		_transform = GetComponent<RectTransform>();
	}

	private void LateUpdate()
	{
		_transform.offsetMin = new Vector2(
			Left.rect.width + Space, Bottom.rect.height + Space
		);
		_transform.offsetMax = new Vector2(
			-Right.rect.width - Space, -Top.rect.height - Space
		);
	}
}
}