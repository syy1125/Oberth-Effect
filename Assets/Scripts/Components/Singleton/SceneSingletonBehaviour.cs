using System.Reflection;
using UnityEngine;

namespace Syy1125.OberthEffect.Components.Singleton
{
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