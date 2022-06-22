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
		var deserializer = new DeserializerBuilder().Build();

		var output = deserializer.Deserialize<TestObject>(@"---
TestField:
  Name: Hello
  Desc: World
  Other:
    - This
    - That
    - And:
        Some: More
        Deeply: Nested
");
		
		Debug.Log(output);
		
	}
}
}