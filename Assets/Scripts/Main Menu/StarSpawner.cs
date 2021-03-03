using System.Collections;
using UnityEngine;

public class StarSpawner : MonoBehaviour
{
	public GameObject StarPrefab;
	public int StarCount = 10;
	public float ResetPeriod = 15f;
	public float MinMass = 0.2f;
	public float MaxMass = 10f;
	public Gradient StarColorGradient;

	private IEnumerator Start()
	{
		while (true)
		{
			GameObject[] stars = SpawnStars();
			foreach (GameObject star in stars)
			{
				StartCoroutine(FadeAlpha(star.GetComponent<Star>(), 1f, 2f));
			}

			yield return new WaitForSeconds(ResetPeriod);

			foreach (GameObject star in stars)
			{
				StartCoroutine(FadeAlpha(star.GetComponent<Star>(), 0f, 2f));
			}

			yield return new WaitForSeconds(2);

			foreach (GameObject star in stars)
			{
				Destroy(star);
			}

			yield return new WaitForSeconds(1);
		}
	}

	private GameObject[] SpawnStars()
	{
		var stars = new GameObject[StarCount];

		var totalMass = 0f;
		Vector3 centerOfMass = Vector3.zero;
		Vector3 centerOfVelocity = Vector3.zero;
		for (var i = 0; i < stars.Length; i++)
		{
			// Not exactly correct but whatever, kinda close
			float logMass = Random.Range(Mathf.Log(MinMass), Mathf.Log(MaxMass));
			float mass = Mathf.Exp(logMass);

			Vector3 position = Random.insideUnitSphere * 10;
			Vector3 velocity = Random.insideUnitSphere;

			totalMass += mass;
			centerOfMass += position * mass / stars.Length;
			centerOfVelocity += velocity * mass / stars.Length;

			stars[i] = Instantiate(StarPrefab, position, Quaternion.identity, transform);
			stars[i].GetComponent<Rigidbody>().velocity = velocity;
			stars[i].GetComponent<Star>().SetMass(mass);
			Color color = StarColorGradient.Evaluate(Mathf.InverseLerp(MinMass, MaxMass, mass));
			color.a = 0f;
			stars[i].GetComponent<Star>().SetColor(color);
		}

		centerOfMass /= totalMass;
		centerOfVelocity /= totalMass;

		foreach (GameObject star in stars)
		{
			star.transform.position -= centerOfMass;
			star.GetComponent<Rigidbody>().velocity -= centerOfVelocity;
		}

		return stars;
	}

	private IEnumerator FadeAlpha(Star star, float alpha, float duration)
	{
		Color color = star.GetComponent<SpriteRenderer>().color;
		float startAlpha = color.a;
		float startTime = Time.time;
		float endTime = startTime + duration;

		while (Time.time < endTime)
		{
			float t = Mathf.InverseLerp(startTime, endTime, Time.time);
			color.a = Mathf.Lerp(startAlpha, alpha, t);
			star.SetColor(color);
			yield return null;
		}

		color.a = alpha;
		star.SetColor(color);
	}
}