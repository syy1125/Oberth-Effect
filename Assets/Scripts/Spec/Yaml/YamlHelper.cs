using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Syy1125.OberthEffect.Spec.Yaml
{
public class YamlMergeException : YamlException
{
	public YamlMergeException(string message) : base(message)
	{}
}

public static class YamlHelper
{
	public static YamlDocument DeepMerge(YamlDocument left, YamlDocument right)
	{
		return new YamlDocument(DeepMerge((YamlMappingNode) left.RootNode, (YamlMappingNode) right.RootNode));
	}

	public static YamlMappingNode DeepMerge(YamlMappingNode left, YamlMappingNode right)
	{
		var output = DeepCopy(left);

		foreach (KeyValuePair<YamlNode, YamlNode> entry in right.Children)
		{
			if (output.Children.TryGetValue(entry.Key, out YamlNode value))
			{
				switch (value)
				{
					case YamlMappingNode leftMappingNode:
						// A mapping node can be merged with another mapping node, or set to null.
						switch (entry.Value)
						{
							case YamlMappingNode rightMappingNode:
								DeepMerge(leftMappingNode, rightMappingNode);
								break;
							case YamlScalarNode rightScalarNode:
								if (rightScalarNode.Value == null)
								{
									output.Children[entry.Key] = new YamlScalarNode(null);
									break;
								}
								else goto default;
							default:
								throw new YamlMergeException(
									$"Cannot merge {value.NodeType}Node with {entry.Value.NodeType}Node"
								);
						}

						break;
					case YamlSequenceNode leftSequenceNode:
						if (entry.Value is YamlSequenceNode rightSequenceNode)
						{
							output.Children[entry.Key] = DeepCopy(rightSequenceNode);
						}
						else
						{
							throw new YamlMergeException(
								$"Cannot merge {value.NodeType}Node with {entry.Value.NodeType}Node"
							);
						}

						break;
					case YamlScalarNode leftScalarNode:
						switch (entry.Value)
						{
							case YamlScalarNode rightScalarNode:
								output.Children[entry.Key] = new YamlScalarNode(rightScalarNode.Value);
								break;
							case YamlMappingNode rightMappingNode:
								if (leftScalarNode.Value == null)
								{
									output.Children[entry.Key] = DeepCopy(rightMappingNode);
									break;
								}
								else goto default;
							default:
								throw new YamlMergeException(
									$"Cannot merge {value.NodeType}Node with {entry.Value.NodeType}Node"
								);
						}

						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(value));
				}
			}
			else
			{
				output.Children.Add(entry.Key, DeepCopy(entry.Value));
			}
		}

		return output;
	}

	public static YamlDocument DeepCopy(YamlDocument document)
	{
		return new YamlDocument(DeepCopy(document.RootNode));
	}

	public static YamlNode DeepCopy(YamlNode node)
	{
		return node switch
		{
			YamlMappingNode mappingNode => DeepCopy(mappingNode),
			YamlSequenceNode sequenceNode => DeepCopy(sequenceNode),
			YamlScalarNode scalarNode => new YamlScalarNode(scalarNode.Value),
			_ => throw new ArgumentOutOfRangeException(nameof(node))
		};
	}

	public static YamlMappingNode DeepCopy(YamlMappingNode node)
	{
		var output = new YamlMappingNode();

		foreach (KeyValuePair<YamlNode, YamlNode> entry in node.Children)
		{
			output.Children[entry.Key] = DeepCopy(entry.Value);
		}

		return output;
	}

	public static YamlNode DeepCopy(YamlSequenceNode node)
	{
		var output = new YamlSequenceNode();

		foreach (YamlNode childNode in node.Children)
		{
			output.Children.Add(DeepCopy(childNode));
		}

		return output;
	}
}
}