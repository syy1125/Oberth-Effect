using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Syy1125.OberthEffect.Spec.Yaml
{
public static class YamlHelper
{
	public static void MergeMappingNodes(YamlMappingNode source, YamlMappingNode overrides)
	{
		foreach (KeyValuePair<YamlNode, YamlNode> entry in overrides.Children)
		{
			if (!source.Children.ContainsKey(entry.Key))
			{
				source.Children[entry.Key] = entry.Value;
				continue;
			}

			YamlNode sourceChild = source.Children[entry.Key];
			YamlMappingNode sourceChildMap = sourceChild as YamlMappingNode;
			YamlMappingNode overrideChildMap = entry.Value as YamlMappingNode;

			if (sourceChildMap != null && overrideChildMap != null)
			{
				MergeMappingNodes(sourceChildMap, overrideChildMap);
			}
			else if (sourceChildMap == null && overrideChildMap == null)
			{
				source.Children[entry.Key] = entry.Value;
			}
			else
			{
				throw new ArgumentException("Cannot merge mapping and non-mapping node");
			}
		}
	}
}
}