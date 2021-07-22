using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Syy1125.OberthEffect.Spec.Yaml
{
/// <summary>
/// Takes a <code>YamlNode</code> and presents it as a parser for typed parsing.
/// </summary>
public class YamlStreamParserAdapter : IParser
{
	private readonly IEnumerator<ParsingEvent> _enumerator;

	public YamlStreamParserAdapter(YamlNode rootNode)
	{
		_enumerator = YamlStreamConverter.ConvertToEventStream(rootNode).GetEnumerator();
	}

	public ParsingEvent Current => _enumerator.Current;

	public bool MoveNext()
	{
		return _enumerator.MoveNext();
	}
}

// Basaed on https://dotnetfiddle.net/jaG1i1
internal static class YamlStreamConverter
{
	public static IEnumerable<ParsingEvent> ConvertToEventStream(YamlNode node)
	{
		switch (node)
		{
			case YamlScalarNode scalar:
				return ConvertToEventStream(scalar);
			case YamlSequenceNode sequence:
				return ConvertToEventStream(sequence);
			case YamlMappingNode mapping:
				return ConvertToEventStream(mapping);
			default:
				throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
		}
	}

	private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlScalarNode scalar)
	{
		yield return new Scalar(scalar.Anchor, scalar.Tag, scalar.Value, scalar.Style, false, false);
	}

	private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlSequenceNode sequence)
	{
		yield return new SequenceStart(sequence.Anchor, sequence.Tag, false, sequence.Style);

		foreach (var node in sequence.Children)
		{
			foreach (var evt in ConvertToEventStream(node))
			{
				yield return evt;
			}
		}

		yield return new SequenceEnd();
	}

	private static IEnumerable<ParsingEvent> ConvertToEventStream(YamlMappingNode mapping)
	{
		yield return new MappingStart(mapping.Anchor, mapping.Tag, false, mapping.Style);

		foreach (var pair in mapping.Children)
		{
			foreach (var evt in ConvertToEventStream(pair.Key))
			{
				yield return evt;
			}

			foreach (var evt in ConvertToEventStream(pair.Value))
			{
				yield return evt;
			}
		}

		yield return new MappingEnd();
	}
}
}