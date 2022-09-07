using System.Reflection;
using UnityEngine;

namespace Syy1125.OberthEffect.Components.Singleton
{
/// <summary>
/// Uses reflection to create a simple and reusable way to define a scene-level singleton.
/// Requires a static property named <c>Instance</c>. Recommended implementation is <c>public static ClassName Instance { get; private set; }</c>.
/// </summary>
public abstract class SceneSingletonBehaviour : MonoBehaviour
{
	private PropertyInfo _instance;

	protected virtual void Awake()
	{
		_instance = GetType().GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
		Debug.Assert(_instance != null, "_instance != null");

		if (_instance.GetValue(null) == null)
		{
			_instance.SetValue(null, this);
		}
		else
		{
			Debug.LogError($"Multiple instantiations of {GetType()}");
			Destroy(this);
		}
	}

	protected virtual void OnDestroy()
	{
		if (ReferenceEquals(_instance.GetValue(null), this))
		{
			_instance.SetValue(null, null);
		}
	}
}
}