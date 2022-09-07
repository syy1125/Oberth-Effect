using System.Collections;
using Syy1125.OberthEffect.Components.Singleton;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(VerticalLayoutGroup))]
public class ToastManager : SceneSingletonBehaviour
{
	public static ToastManager Instance { get; private set; }

	public GameObject ToastPrefab;

	public float MoveTime;
	public float FadeInTime;
	public float StayTime;
	public float FadeOutTime;

	private RectTransform _transform;
	private VerticalLayoutGroup _layout;

	protected override void Awake()
	{
		base.Awake();
		_transform = GetComponent<RectTransform>();
		_layout = GetComponent<VerticalLayoutGroup>();
	}

	public void CreateToast(string message)
	{
		StartCoroutine(DoToastLifecycle(message));
	}

	private IEnumerator DoToastLifecycle(string message)
	{
		GameObject toast = Instantiate(ToastPrefab, transform);
		toast.transform.SetAsFirstSibling();

		toast.GetComponentInChildren<TMP_Text>().text = message;

		var toastTransform = toast.GetComponent<RectTransform>();
		var toastLayout = toast.GetComponent<LayoutElement>();

		foreach (Graphic graphic in toastLayout.GetComponentsInChildren<Graphic>())
		{
			graphic.CrossFadeAlpha(0f, 0f, true);
		}

		if (transform.childCount > 1)
		{
			yield return CoroutineUtils.LerpOverUnscaledTime(
				-_layout.spacing, toastLayout.preferredHeight, MoveTime,
				height =>
				{
					toastTransform.offsetMin = new(toastTransform.offsetMin.x, -height / 2);
					toastTransform.offsetMax = new(toastTransform.offsetMax.x, height / 2);
					LayoutRebuilder.MarkLayoutForRebuild(_transform);
				}
			);
		}

		foreach (Graphic graphic in toastLayout.GetComponentsInChildren<Graphic>())
		{
			graphic.CrossFadeAlpha(1f, FadeInTime, true);
		}

		yield return new WaitForSecondsRealtime(FadeInTime + StayTime);

		foreach (Graphic graphic in toastLayout.GetComponentsInChildren<Graphic>())
		{
			graphic.CrossFadeAlpha(0f, FadeOutTime, true);
		}

		yield return new WaitForSecondsRealtime(FadeOutTime);

		Destroy(toastLayout.gameObject);
	}
}
}