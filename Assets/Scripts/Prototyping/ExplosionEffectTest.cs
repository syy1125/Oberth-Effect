using Syy1125.OberthEffect.WeaponEffect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syy1125.OberthEffect.Prototyping
{
public class ExplosionEffectTest : MonoBehaviour
{
	private Vector2 _min;
	private Vector2 _max;

	private void Awake()
	{
		_min = new Vector2(-0.5f, -0.5f);
		_max = new Vector2(0.5f, 0.5f);
	}

	private void Update()
	{
		if (Mouse.current.leftButton.wasPressedThisFrame)
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			position.z = 0f;
			ExplosionManager.Instance.PlayEffectAt(position, 1f, null);

			Debug.Log(ExplosionManager.CalculateDamageFactor(_min, _max, position, 0.5f));
		}

		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			Vector3 position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			position.z = 0f;
			ExplosionManager.Instance.PlayEffectAt(position, 2f, null);

			Debug.Log(ExplosionManager.CalculateDamageFactor(_min, _max, position, 1f));
		}
	}
}
}