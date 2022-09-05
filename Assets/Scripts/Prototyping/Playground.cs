using System;
using System.Collections;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Spec.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	private IEnumerator Start()
	{
		GetComponent<ParticleSystem>().Play();
		var main = GetComponent<ParticleSystem>().main;
		main.playOnAwake = true;
		Invoke(nameof(Reenable), 5);
		yield return new WaitForSeconds(2);
		gameObject.SetActive(false);
	}

	public void Reenable()
	{
		gameObject.SetActive(true);
	}
}
}