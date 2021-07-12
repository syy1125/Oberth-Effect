using Syy1125.OberthEffect.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class ExplosionEffectTest : MonoBehaviour
{
	private void Update()
	{
		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			position.z = 0f;
			ExplosionEffectManager.Instance.PlayEffectAt(position, 1f);
		}

		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			position.z = 0f;
			ExplosionEffectManager.Instance.PlayEffectAt(position, 2f);
		}
	}
}
}