using UnityEngine;

namespace Syy1125.OberthEffect.Guide
{
[RequireComponent(typeof(CanvasGroup))]
public class Blink : MonoBehaviour
{
	public float Period = 1f;

	private void Update()
	{
		float modTime = Time.unscaledTime % Period;
		GetComponent<CanvasGroup>().alpha = modTime < Period / 2 ? 1f : 0f;
	}
}
}