using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Prototyping
{
public class Playground : MonoBehaviour
{
	class TestObject
	{
		public Dictionary<string, object> TestField;
	}

	private void Start()
	{
		Debug.Log(JsonUtility.ToJson(new Color(1f, 0f, 0f, 1f)));
	}
}
}