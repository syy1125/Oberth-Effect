using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace Syy1125.OberthEffect.Components.Singleton
{
public abstract class SceneSingletonBehaviourPun<T> : MonoBehaviourPun where T : SceneSingletonBehaviourPun<T>
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