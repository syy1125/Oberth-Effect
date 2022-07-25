using System;
using System.Collections.Generic;
using System.IO;
using Syy1125.OberthEffect.CombatSystem;
using Syy1125.OberthEffect.Spec;
using Syy1125.OberthEffect.Spec.Block;
using Syy1125.OberthEffect.Spec.ModLoading;
using Syy1125.OberthEffect.Spec.Yaml;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Syy1125.OberthEffect.Prototyping
{
public struct TestStruct
{
	public string Name;
	public float Value;
	public Vector2 Position;
}

public class TestComponent : INetworkedProjectileComponent<TestStruct>
{
	public void LoadSpec(TestStruct spec)
	{
	}
}

public class TaggedSerializationTest : MonoBehaviour
{
	public TMP_Text Input;
	public TMP_Text Output;

	private void Start()
	{
		NetworkedProjectileConfig.Register<TestStruct, TestComponent>("testtag");
		
		var serializerBuilder = new SerializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter());

		var deserializerBuilder = new DeserializerBuilder()
			.WithTypeConverter(new Vector2TypeConverter())
			.WithTypeConverter(new Vector2IntTypeConverter())
			.WithObjectFactory(new TextureSpecFactory(new BlockSpecFactory(new DefaultObjectFactory())));

		ModLoader.OnAugmentSerializer?.Invoke(serializerBuilder, deserializerBuilder);

		var serializer = serializerBuilder.Build();
		var deserializer = deserializerBuilder.Build();

		var input = new StringReader(Input.text);
		var value = deserializer.Deserialize(input);

		Output.text = serializer.Serialize(value);
	}
}
}