using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Syy1125.OberthEffect.Spec.Yaml
{
public sealed class Vector2TypeConverter : IYamlTypeConverter
{
	public bool Accepts(Type type)
	{
		return type == typeof(Vector2);
	}

	public object ReadYaml(IParser parser, Type type)
	{
		if (!parser.Accept<MappingStart>()) throw new InvalidDataException();
		parser.MoveNext();

		Vector2 output = new Vector2();

		ReadNode(parser, ref output);
		ReadNode(parser, ref output);

		if (!parser.Accept<MappingEnd>()) throw new InvalidDataException();
		parser.MoveNext();

		return output;
	}

	private static void ReadNode(IParser parser, ref Vector2 output)
	{
		var current = parser.Current as Scalar;
		if (current == null) throw new InvalidDataException();
		parser.MoveNext();

		string key = current.Value;

		current = parser.Current as Scalar;
		if (current == null) throw new InvalidDataException();
		parser.MoveNext();

		if (!float.TryParse(current.Value, out float value)) throw new InvalidDataException();

		switch (key)
		{
			case "X":
				output.x = value;
				break;
			case "Y":
				output.y = value;
				break;
			default:
				throw new InvalidDataException($"Unexpected key {key} when parsing Vector2");
		}
	}

	public void WriteYaml(IEmitter emitter, object value, Type type)
	{
		Vector2 vector = (Vector2) value;

		emitter.Emit(new MappingStart(null, null, false, MappingStyle.Flow));

		emitter.Emit(new Scalar(null, "X"));
		emitter.Emit(new Scalar(null, vector.x.ToString(CultureInfo.InvariantCulture)));

		emitter.Emit(new Scalar(null, "Y"));
		emitter.Emit(new Scalar(null, vector.y.ToString(CultureInfo.InvariantCulture)));

		emitter.Emit(new MappingEnd());
	}
}

public sealed class Vector2IntTypeConverter : IYamlTypeConverter
{
	public bool Accepts(Type type)
	{
		return type == typeof(Vector2Int);
	}

	public object ReadYaml(IParser parser, Type type)
	{
		if (!parser.Accept<MappingStart>()) throw new InvalidDataException();
		parser.MoveNext();

		Vector2Int output = new Vector2Int();

		ReadNode(parser, ref output);
		ReadNode(parser, ref output);

		if (!parser.Accept<MappingEnd>()) throw new InvalidDataException();
		parser.MoveNext();

		return output;
	}

	private static void ReadNode(IParser parser, ref Vector2Int output)
	{
		var current = parser.Current as Scalar;
		if (current == null) throw new InvalidDataException();
		parser.MoveNext();

		string key = current.Value;

		current = parser.Current as Scalar;
		if (current == null) throw new InvalidDataException();
		parser.MoveNext();

		if (!int.TryParse(current.Value, out int value)) throw new InvalidDataException();

		switch (key)
		{
			case "X":
				output.x = value;
				break;
			case "Y":
				output.y = value;
				break;
			default:
				throw new InvalidDataException($"Unexpected key {key} when parsing Vector2Int");
		}
	}

	public void WriteYaml(IEmitter emitter, object value, Type type)
	{
		Vector2 vector = (Vector2) value;

		emitter.Emit(new MappingStart(null, null, false, MappingStyle.Flow));

		emitter.Emit(new Scalar(null, "X"));
		emitter.Emit(new Scalar(null, vector.x.ToString(CultureInfo.InvariantCulture)));

		emitter.Emit(new Scalar(null, "Y"));
		emitter.Emit(new Scalar(null, vector.y.ToString(CultureInfo.InvariantCulture)));

		emitter.Emit(new MappingEnd());
	}
}

/// <summary>
/// Only used for serialization. Default deserialization works just fine.
/// </summary>
public sealed class ColorTypeConverter : IYamlTypeConverter
{
	public bool Accepts(Type type)
	{
		return type == typeof(Color);
	}

	public object ReadYaml(IParser parser, Type type)
	{
		throw new NotImplementedException();
	}

	public void WriteYaml(IEmitter emitter, object value, Type type)
	{
		var color = (Color) value;
		
		emitter.Emit(new MappingStart(null, null, false, MappingStyle.Any));
		
		emitter.Emit(new Scalar(null, "r"));
		emitter.Emit(new Scalar(null, color.r.ToString(CultureInfo.InvariantCulture)));
		
		emitter.Emit(new Scalar(null, "g"));
		emitter.Emit(new Scalar(null, color.g.ToString(CultureInfo.InvariantCulture)));
		
		emitter.Emit(new Scalar(null, "b"));
		emitter.Emit(new Scalar(null, color.b.ToString(CultureInfo.InvariantCulture)));
		
		emitter.Emit(new Scalar(null, "a"));
		emitter.Emit(new Scalar(null, color.a.ToString(CultureInfo.InvariantCulture)));
		
		emitter.Emit(new MappingEnd());
	}
}
}