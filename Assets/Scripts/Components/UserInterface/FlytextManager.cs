using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Syy1125.OberthEffect.Components.UserInterface
{
[RequireComponent(typeof(RectTransform))]
public class FlytextManager : MonoBehaviour
{
	public GameObject FlytextPrefab;
	public int MaxInstances = 3;
	public float RiseTime = 1f;
	public float RiseDistance = 10f;
	public float StayTime = 1.5f;
	public float FadeTime = 0.5f;

	private Queue<Tuple<GameObject, Coroutine>> _instances = new Queue<Tuple<GameObject, Coroutine>>();

	public void CreateNotificationFlytext(Vector3 worldPosition, string text)
	{
		Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
			    GetComponent<RectTransform>(), screenPoint, Camera.main, out Vector2 localPoint
		    ))
		{
			GameObject flytext = Instantiate(FlytextPrefab, transform);
			flytext.transform.localPosition = localPoint;
			flytext.GetComponent<Text>().text = text;
			var coroutine = StartCoroutine(ManageNotificationFlytext(flytext));

			_instances.Enqueue(new Tuple<GameObject, Coroutine>(flytext, coroutine));

			while (_instances.Count > MaxInstances)
			{
				(flytext, coroutine) = _instances.Dequeue();
				Destroy(flytext);
				StopCoroutine(coroutine);
			}
		}
	}

	private IEnumerator ManageNotificationFlytext(GameObject flytext)
	{
		float startTime = Time.time;
		float endTime = startTime + RiseTime;
		Vector3 startPosition = flytext.transform.localPosition;
		Vector3 endPosition = startPosition + new Vector3(0, RiseDistance, 0);

		while (Time.time < endTime)
		{
			flytext.transform.localPosition = Vector3.Lerp(
				startPosition, endPosition, Mathf.InverseLerp(startTime, endTime, Time.time)
			);
			yield return null;
		}

		flytext.transform.localPosition = endPosition;

		yield return new WaitForSecondsRealtime(StayTime);
		flytext.GetComponent<Text>().CrossFadeAlpha(0f, FadeTime, true);
		yield return new WaitForSecondsRealtime(FadeTime);
		Destroy(flytext);
	}
}
}